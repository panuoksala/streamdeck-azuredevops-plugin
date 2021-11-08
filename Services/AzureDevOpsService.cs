using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;
using StreamDeckAzureDevOps.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StreamDeckAzureDevOps.Services
{
    public class AzureDevOpsService
    {
        public async Task<string> GetBuildStatusImage(AzureDevOpsSettingsModel settings)
        {
            var connection = GetConnection(settings);
            var buildClient = connection.GetClient<BuildHttpClient>();
            var projectClient = connection.GetClient<ProjectHttpClient>();

            var teamProject = await projectClient.GetProject(settings.ProjectName);

            Build build;
            if (settings.DefinitionId > 0)
            {
                build = await buildClient.GetLatestBuildAsync(teamProject.Id, settings.DefinitionId.ToString());

                // BUG in the API, in progress builds are skipped for some reason when using top 1.
                var latestInProgressBuilds = await buildClient.GetBuildsAsync(
                    teamProject.Id,
                    top: 1,
                    queryOrder: BuildQueryOrder.QueueTimeDescending,
                    definitions: new[] { settings.DefinitionId },
                    statusFilter: BuildStatus.InProgress);
                var latestInProgressBuild = latestInProgressBuilds?.FirstOrDefault();
                if (latestInProgressBuild != null && (build == null || latestInProgressBuild.Id > build.Id))
                {
                    build = latestInProgressBuild;
                }
            }
            else
            {
                // Get latest in-progress and ignore it if it's older than 1 day (waiting for approval, most likely).
                var latestInProgressBuilds = await buildClient.GetBuildsAsync(
                    teamProject.Id,
                    top: 1,
                    queryOrder: BuildQueryOrder.QueueTimeDescending,
                    statusFilter: BuildStatus.InProgress);
                build = latestInProgressBuilds?.FirstOrDefault(x => x.StartTime > DateTime.UtcNow.AddDays(-1));

                // No in progress builds.
                if (build == null)
                {
                    // Get latest build.
                    var latestBuild = await buildClient.GetBuildsAsync(teamProject.Id, top: 1, queryOrder: BuildQueryOrder.QueueTimeDescending);
                    build = latestBuild?.FirstOrDefault();
                }
            }

            return GetBuildStatusImage(build);
        }

        public async Task StartBuild(AzureDevOpsSettingsModel settings)
        {
            var connection = GetConnection(settings);
            var buildClient = connection.GetClient<BuildHttpClient>();
            var projectClient = connection.GetClient<ProjectHttpClient>();

            var buildDefinition = await buildClient.GetDefinitionAsync(settings.ProjectName, settings.DefinitionId);
            var teamProject = await projectClient.GetProject(settings.ProjectName);

            await buildClient.QueueBuildAsync(new Build() { Definition = buildDefinition, Project = teamProject });
        }

        public async Task StartRelease(AzureDevOpsSettingsModel settings)
        {
            var connection = GetConnection(settings);
            var releaseClient = connection.GetClient<ReleaseHttpClient2>();
            var releaseMetaData = new Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.ReleaseStartMetadata
            {
                DefinitionId = settings.DefinitionId,
            };

            await releaseClient.CreateReleaseAsync(releaseMetaData, settings.ProjectName);
        }

        private static string GetBuildStatusImage(Build build)
        {
            // All supported build states (All and None are not supported because I don't know what they would mean).
            return build?.Status switch
            {
                BuildStatus.Completed when build?.Result == BuildResult.Succeeded => "images/Azure-DevOps-success.png",
                BuildStatus.Completed when build?.Result == BuildResult.Failed => "images/Azure-DevOps-fail.png",
                BuildStatus.Completed when build?.Result == BuildResult.Canceled => "images/Azure-DevOps-cancel.png",
                BuildStatus.Completed when build?.Result == BuildResult.PartiallySucceeded => "images/Azure-DevOps-partial-success.png",
                BuildStatus.Cancelling => "images/Azure-DevOps-cancel.png",
                BuildStatus.InProgress => "images/Azure-DevOps-in-progress.png",
                BuildStatus.NotStarted => "images/Azure-DevOps-waiting.png",
                BuildStatus.Postponed => "images/Azure-DevOps-waiting.png",
                _ => "images/Azure-DevOps-unknown.png",
            };
        }

        private VssConnection GetConnection(AzureDevOpsSettingsModel settings)
        {
            var credentials = new VssBasicCredential(string.Empty, settings.PAT);
            return new VssConnection(new Uri($"https://dev.azure.com/{settings.OrganizationName}"), credentials);
        }
    }
}
