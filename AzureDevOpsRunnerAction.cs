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
                        await BuildStatus(connection, args.context);
                        break;
                    case PipelineType.Release:
                        await StartRelease(connection);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unsupported pipeline type {SettingsModel.PipelineType}.");
                }

                await Manager.ShowOkAsync(args.context);

                await Manager.SetSettingsAsync(args.context, SettingsModel);
            }
            catch (Exception)
            {
                await Manager.ShowAlertAsync(args.context);
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
                        await BuildStatus(connection, args.context);
                        break;
                    case PipelineType.Release:
                        //await StartRelease(connection);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unsupported pipeline type {SettingsModel.PipelineType}.");
                }

                await Manager.ShowOkAsync(args.context);

                await Manager.SetSettingsAsync(args.context, SettingsModel);
            }
            catch (Exception)
            {
                await Manager.ShowAlertAsync(args.context);
            }
        }

        public async Task CheckBuildStatus(string context, CancellationToken ct)
        {
            try
            {
                await Manager.SetTitleAsync(context, "Background task...");

                while (!ct.IsCancellationRequested)
                {
                    var credentials = new VssBasicCredential(string.Empty, SettingsModel.PAT);
                    var connection = new VssConnection(new Uri($"https://dev.azure.com/{SettingsModel.OrganizationName}"), credentials);
                    await BuildStatus(connection, context);

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
            catch
            {
            }
        }

        public async Task BuildStatus(VssConnection connection, string context)
        {
            await Manager.SetTitleAsync(context, "Updating...");

            var buildClient = connection.GetClient<BuildHttpClient>();
            var projectClient = connection.GetClient<ProjectHttpClient>();

            var teamProject = await projectClient.GetProject(SettingsModel.ProjectName);

            var build = await buildClient.GetLatestBuildAsync(teamProject.Id, SettingsModel.DefinitionId.ToString());

            // BUG in the API, in progress builds are skipped for some reason when using top 1.
            var latestInProgressBuilds = await buildClient.GetBuildsAsync(teamProject.Id, top: 1, statusFilter: Microsoft.TeamFoundation.Build.WebApi.BuildStatus.InProgress);
            var latestInProgressBuild = latestInProgressBuilds?.FirstOrDefault();
            if (latestInProgressBuild != null && (build == null || latestInProgressBuild.Id > build.Id))
            {
                build = latestInProgressBuild;
            }

            string status = null;
            if (build != null)
            {
                status = build.Status.ToString();
                if (build.Status == Microsoft.TeamFoundation.Build.WebApi.BuildStatus.Completed)
                {
                    status = build.Result?.ToString();
                }
            }

            await Manager.SetTitleAsync(context, status ?? "Unknown");
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
