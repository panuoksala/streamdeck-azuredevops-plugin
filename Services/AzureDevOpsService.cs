using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;
using StreamDeckAzureDevOps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Deployment = Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Deployment;

namespace StreamDeckAzureDevOps.Services
{
    public class AzureDevOpsService
    {
        private readonly ILogger logger;

        public TimeSpan StaleInProgressBuild { get; set; } = TimeSpan.FromDays(1);
        public TimeSpan StaleInProgressDeployment { get; set; } = TimeSpan.FromDays(1);

        public AzureDevOpsService(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<string> GetBuildStatusImage(AzureDevOpsSettingsModel settings)
        {
            try
            {
                var connection = GetConnection(settings);
                var buildClient = connection.GetClient<BuildHttpClient>();
                var projectClient = connection.GetClient<ProjectHttpClient>();

                var teamProject = await projectClient.GetProject(settings.ProjectName);

                // Definition ID == 0 means that we take whatever build definitions are available.
                Build build;
                if (settings.DefinitionId > 0)
                {
                    // First check the latest in progress build.
                    // It's more useful to show in progress build than latest one because
                    // latest build might be waiting for in progress one to complete.
                    var latestInProgressBuilds = await buildClient.GetBuildsAsync(
                        teamProject.Id,
                        top: 1,
                        queryOrder: BuildQueryOrder.QueueTimeDescending,
                        definitions: new[] { settings.DefinitionId },
                        statusFilter: BuildStatus.InProgress,
                        branchName: settings.GetFullBranchName());

                    // Ignore if it's 1 day old. (probably waiting for approval)
                    build = latestInProgressBuilds?.FirstOrDefault(x => x.StartTime > DateTime.UtcNow.Subtract(StaleInProgressBuild));
                    if (build == null)
                    {
                        // Get latest build if there are no active builds.
                        build = await buildClient.GetLatestBuildAsync(
                            teamProject.Id,
                            settings.DefinitionId.ToString(),
                            branchName: settings.GetFullBranchName());
                    }
                }
                else
                {
                    // Get latest in-progress builds.
                    var latestInProgressBuilds = await buildClient.GetBuildsAsync(
                        teamProject.Id,
                        top: 1,
                        queryOrder: BuildQueryOrder.QueueTimeDescending,
                        statusFilter: BuildStatus.InProgress);

                    // Ignore if it's 1 day old. (probably waiting for approval)
                    build = latestInProgressBuilds?.FirstOrDefault(x => x.StartTime > DateTime.UtcNow.Subtract(StaleInProgressBuild));

                    // No in progress builds.
                    if (build == null)
                    {
                        // Get latest build if there are no active builds.
                        var latestBuild = await buildClient.GetBuildsAsync(teamProject.Id, top: 1, queryOrder: BuildQueryOrder.QueueTimeDescending);
                        build = latestBuild?.FirstOrDefault();
                    }
                }

                return GetBuildStatusImage(build);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get build status for build status image.");
                return string.Empty;
            }
        }

        public async Task StartBuild(AzureDevOpsSettingsModel settings)
        {
            try
            {
                var connection = GetConnection(settings);
                var buildClient = connection.GetClient<BuildHttpClient>();
                var projectClient = connection.GetClient<ProjectHttpClient>();

                var teamProjectTask = projectClient.GetProject(settings.ProjectName);

                List<BuildDefinitionReference> buildDefinitions;
                if (settings.DefinitionId > 0)
                {
                    BuildDefinition buildDefinition = await buildClient.GetDefinitionAsync(settings.ProjectName, settings.DefinitionId);
                    if (buildDefinition == null)
                    {
                        throw new ArgumentException($"Build definition {settings.DefinitionId} not found");
                    }

                    buildDefinitions = new List<BuildDefinitionReference>
                {
                    buildDefinition
                };
                }
                else
                {
                    buildDefinitions = await buildClient.GetDefinitionsAsync(settings.ProjectName);
                    if (buildDefinitions?.Any() != true)
                    {
                        throw new ArgumentException($"No build definitions found");
                    }
                }

                var teamProject = await teamProjectTask;
                foreach (var buildDef in buildDefinitions)
                {
                    await buildClient.QueueBuildAsync(new Build() { Definition = buildDef, Project = teamProject, SourceBranch = settings.GetFullBranchName() });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to start build {settings.DefinitionId}. Please check build settings.");
            }
        }

        public async Task<string> GetReleaseStatusImage(AzureDevOpsSettingsModel settings)
        {
            try
            {
                var connection = GetConnection(settings);
                var releaseClient = connection.GetClient<ReleaseHttpClient2>();
                var projectClient = connection.GetClient<ProjectHttpClient>();

                var teamProject = await projectClient.GetProject(settings.ProjectName);

                int? definitionId = settings.DefinitionId > 0
                    ? settings.DefinitionId
                    : null;

                // Prioritize in-progress deployments over waiting/completed.
                List<Deployment> releases = await releaseClient.GetDeploymentsAsync(
                        teamProject.Id,
                        definitionId: definitionId,
                        queryOrder: ReleaseQueryOrder.Descending,
                        deploymentStatus: DeploymentStatus.InProgress);

                Deployment deployment = releases?.FirstOrDefault(x => x.QueuedOn > DateTime.UtcNow.Subtract(StaleInProgressBuild));
                if (deployment == null)
                {
                    releases = await releaseClient.GetDeploymentsAsync(
                        teamProject.Id,
                        definitionId: definitionId,
                        queryOrder: ReleaseQueryOrder.Descending);
                    deployment = releases?.FirstOrDefault();
                }

                return GetBuildStatusImage(deployment);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get release status image.");
                return string.Empty;
            }
        }

        public async Task StartRelease(AzureDevOpsSettingsModel settings)
        {
            try
            {
                var connection = GetConnection(settings);
                var releaseClient = connection.GetClient<ReleaseHttpClient2>();

                List<int> definitionIds = new List<int>();
                if (settings.DefinitionId > 0)
                {
                    definitionIds.Add(settings.DefinitionId);
                }
                else
                {
                    var projectClient = connection.GetClient<ProjectHttpClient>();
                    var teamProject = await projectClient.GetProject(settings.ProjectName);

                    var releaseDefinitions = await releaseClient.GetReleaseDefinitionsAsync(teamProject.Id);
                    if (releaseDefinitions?.Any() != true)
                    {
                        throw new ArgumentException($"No release definitions found");
                    }

                    definitionIds.AddRange(releaseDefinitions.Select(x => x.Id));
                }

                foreach (var definitionId in definitionIds)
                {
                    var releaseMetaData = new ReleaseStartMetadata
                    {
                        DefinitionId = definitionId,
                    };

                    await releaseClient.CreateReleaseAsync(releaseMetaData, settings.ProjectName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to start release {settings.DefinitionId}.");
            }
        }

        private static string GetBuildStatusImage(Build build)
        {
            // All supported build states (All and None are not supported because I don't know what they would mean).
            return build?.Status switch
            {
                null => "images/Azure-DevOps-unknown.png",
                BuildStatus.Completed when build.Result == BuildResult.Succeeded => "images/Azure-DevOps-success.png",
                BuildStatus.Completed when build.Result == BuildResult.Failed => "images/Azure-DevOps-fail.png",
                BuildStatus.Completed when build.Result == BuildResult.Canceled => "images/Azure-DevOps-cancel.png",
                BuildStatus.Completed when build.Result == BuildResult.PartiallySucceeded => "images/Azure-DevOps-partial-success.png",
                BuildStatus.Cancelling => "images/Azure-DevOps-cancel.png",
                BuildStatus.InProgress => "images/Azure-DevOps-in-progress.png",
                BuildStatus.NotStarted => "images/Azure-DevOps-waiting.png",
                BuildStatus.Postponed => "images/Azure-DevOps-waiting.png",
                _ => "images/Azure-DevOps-unknown.png",
            };
        }

        private static string GetBuildStatusImage(Deployment deployment)
        {
            // All supported deployment states.
            return deployment?.DeploymentStatus switch
            {
                null => "images/Azure-DevOps-unknown.png",
                DeploymentStatus.Succeeded => "images/Azure-DevOps-success.png",
                DeploymentStatus.PartiallySucceeded => "images/Azure-DevOps-partial-success.png",
                DeploymentStatus.Failed => "images/Azure-DevOps-fail.png",
                _ when deployment.OperationStatus is DeploymentOperationStatus.Canceled or DeploymentOperationStatus.Cancelling => "images/Azure-DevOps-cancel.png",
                _ when deployment.OperationStatus is DeploymentOperationStatus.Pending
                    or DeploymentOperationStatus.Scheduled
                    or DeploymentOperationStatus.ManualInterventionPending
                    or DeploymentOperationStatus.Queued
                    or DeploymentOperationStatus.QueuedForPipeline
                    or DeploymentOperationStatus.QueuedForAgent => "images/Azure-DevOps-waiting.png",
                _ when deployment.OperationStatus is DeploymentOperationStatus.PhaseInProgress => "images/Azure-DevOps-in-progress.png",
                DeploymentStatus.InProgress => "images/Azure-DevOps-in-progress.png",
                _ => "images/Azure-DevOps-unknown.png",
            };
        }

        private VssConnection GetConnection(AzureDevOpsSettingsModel settings)
        {
            var credentials = new VssBasicCredential(string.Empty, settings.PAT);
            if (settings.OrganizationURL.Contains("https://"))
            {
                return new VssConnection(new Uri(settings.OrganizationURL), credentials);
            }
            else
            {
                return new VssConnection(new Uri($"https://{settings.OrganizationURL}"), credentials);
            }

        }
    }
}
