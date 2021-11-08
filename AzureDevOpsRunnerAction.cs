using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using StreamDeckAzureDevOps.Models;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using System.Threading;
using System.Linq;

using BuildStatus = Microsoft.TeamFoundation.Build.WebApi.BuildStatus;

namespace StreamDeckAzureDevOps
{
    [ActionUuid(Uuid = "net.oksala.azuredevops.runner")]
    public class AzureDevOpsRunnerAction : BaseStreamDeckActionWithSettingsModel<Models.AzureDevOpsSettingsModel>
    {
        private CancellationTokenSource _stopApp;

        public override async Task OnKeyUp(StreamDeckEventPayload args)
        {
            try
            {
                // Connect to Azure DevOps Services
                var credentials = new VssBasicCredential(string.Empty, SettingsModel.PAT);
                var connection = new VssConnection(new Uri($"https://dev.azure.com/{SettingsModel.OrganizationName}"), credentials);

                switch ((PipelineType)SettingsModel.PipelineType)
                {
                    case PipelineType.Build:
                        await UpdateStatus(connection, args.context);
                        break;
                    case PipelineType.Release:
                        await StartRelease(connection);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unsupported pipeline type {SettingsModel.PipelineType}.");
                }
                
                await Manager.SetSettingsAsync(args.context, SettingsModel);
            }
            catch (Exception)
            {
                await Manager.ShowAlertAsync(args.context);
                await Manager.SetImageAsync(args.context, "images/Azure-DevOps-unknown.png");
            }
        }

        public override async Task OnWillDisappear(StreamDeckEventPayload args)
        {
            try
            {
                if (_stopApp != null)
                {
                    _stopApp.Cancel();
                    _stopApp = null;
                }
            }
            catch { }
        }

        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            try
            {
                if (_stopApp != null)
                {
                    _stopApp.Cancel();
                }

                _stopApp = new CancellationTokenSource();

                Task.Run(() => CheckBuildStatus(args.context, _stopApp.Token));

                // Connect to Azure DevOps Services
                var credentials = new VssBasicCredential(string.Empty, SettingsModel.PAT);
                var connection = new VssConnection(new Uri($"https://dev.azure.com/{SettingsModel.OrganizationName}"), credentials);

                switch ((PipelineType)SettingsModel.PipelineType)
                {
                    case PipelineType.Build:
                        await UpdateStatus(connection, args.context);
                        break;
                    case PipelineType.Release:
                        //await StartRelease(connection);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unsupported pipeline type {SettingsModel.PipelineType}.");
                }

                await Manager.SetSettingsAsync(args.context, SettingsModel);
            }
            catch (Exception)
            {
                await Manager.ShowAlertAsync(args.context);
                await Manager.SetImageAsync(args.context, "images/Azure-DevOps-unknown.png");
            }
        }

        public async Task CheckBuildStatus(string context, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var credentials = new VssBasicCredential(string.Empty, SettingsModel.PAT);
                    var connection = new VssConnection(new Uri($"https://dev.azure.com/{SettingsModel.OrganizationName}"), credentials);
                    await UpdateStatus(connection, context);

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
            catch
            {
            }
        }

        public async Task UpdateStatus(VssConnection connection, string context)
        {
            await Manager.SetImageAsync(context, "images/Azure-DevOps-updating.png");

            var buildClient = connection.GetClient<BuildHttpClient>();
            var projectClient = connection.GetClient<ProjectHttpClient>();

            var teamProject = await projectClient.GetProject(SettingsModel.ProjectName);

            Build build;
            if (SettingsModel.DefinitionId > 0)
            {
                build = await buildClient.GetLatestBuildAsync(teamProject.Id, SettingsModel.DefinitionId.ToString());

                // BUG in the API, in progress builds are skipped for some reason when using top 1.
                var latestInProgressBuilds = await buildClient.GetBuildsAsync(
                    teamProject.Id,
                    top: 1,
                    queryOrder: BuildQueryOrder.QueueTimeDescending,
                    definitions: new[] { SettingsModel.DefinitionId },
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

            string statusImage = GetBuildStatusImage(build);
            await Manager.SetImageAsync(context, statusImage);

            string status = null;
            if (build != null)
            {
                status = build.Status.ToString();
                if (build.Status == BuildStatus.Completed)
                {
                    status = build.Result?.ToString();
                }
            }

            await Manager.SetTitleAsync(context, status ?? "Unknown");
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

        private async Task StartBuild(VssConnection connection)
        {
            var buildClient = connection.GetClient<BuildHttpClient>();
            var projectClient = connection.GetClient<ProjectHttpClient>();

            var buildDefinition = await buildClient.GetDefinitionAsync(SettingsModel.ProjectName, SettingsModel.DefinitionId);
            var teamProject = await projectClient.GetProject(SettingsModel.ProjectName);

            await buildClient.QueueBuildAsync(new Build() { Definition = buildDefinition, Project = teamProject });
        }

        private async Task StartRelease(VssConnection connection)
        {
            var releaseClient = connection.GetClient<ReleaseHttpClient2>();
            var releaseMetaData = new Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.ReleaseStartMetadata
            {
                DefinitionId = SettingsModel.DefinitionId,
            };
            await releaseClient.CreateReleaseAsync(releaseMetaData, SettingsModel.ProjectName);
        }

        public override async Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            await base.OnDidReceiveSettings(args);
        }

    }
}
