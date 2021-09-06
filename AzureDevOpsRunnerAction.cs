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

namespace StreamDeckAzureDevOps
{
    [ActionUuid(Uuid = "net.oksala.azuredevops.runner.DefaultPluginAction")]
    public class AzureDevOpsRunnerAction : BaseStreamDeckActionWithSettingsModel<Models.AzureDevOpsSettingsModel>
    {
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
                        await StartBuild(connection);
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

        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);
        }

    }
}
