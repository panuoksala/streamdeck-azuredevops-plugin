using StreamDeckAzureDevOps.Models;
using StreamDeckAzureDevOps.Services;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckAzureDevOps
{
    [ActionUuid(Uuid = "net.oksala.azuredevops.runner")]
    public class AzureDevOpsRunnerAction : BaseAction
    {
        private readonly AzureDevOpsService _service = new AzureDevOpsService();

        public async Task UpdateStatus(string context)
        {
            await Manager.SetImageAsync(context, "images/Azure-DevOps-updating.png");

            string statusImage = null;
            switch ((PipelineType)SettingsModel.PipelineType)
            {
                case PipelineType.Build:
                    statusImage = await _service.GetBuildStatusImage(SettingsModel);
                    break;

                case PipelineType.Release:
                    //await _service.StartRelease(SettingsModel);
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unsupported pipeline type {SettingsModel.PipelineType}.");
            }

            if (statusImage != null)
            {
                await Manager.SetImageAsync(context, statusImage);
            }
        }

        public override async Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            await base.OnDidReceiveSettings(args);
        }

        public override async Task UpdateDisplay(StreamDeckEventPayload args)
        {
            await UpdateStatus(args.context);
        }

        public override async Task OnTap(StreamDeckEventPayload args)
        {
            await ExecuteKeyPress(args, (KeyPressAction)SettingsModel.TapAction);
        }

        public override async Task OnLongPress(StreamDeckEventPayload args)
        {
            await ExecuteKeyPress(args, (KeyPressAction)SettingsModel.LongPressAction);
        }

        public override async Task OnError(StreamDeckEventPayload args, Exception ex)
        {
            await Manager.ShowAlertAsync(args.context);
            await Manager.SetImageAsync(args.context, "images/Azure-DevOps-unknown.png");
        }

        public override bool IsSettingsValid()
        {
            return !string.IsNullOrWhiteSpace(SettingsModel.ProjectName)
                && !string.IsNullOrWhiteSpace(SettingsModel.OrganizationName)
                && !string.IsNullOrWhiteSpace(SettingsModel.PAT);
        }

        private async Task ExecuteKeyPress(StreamDeckEventPayload args, KeyPressAction keyPressAction)
        {
            PipelineType pipelineType = (PipelineType)SettingsModel.PipelineType;
            switch (keyPressAction)
            {
                case KeyPressAction.DoNothing:
                    break;

                case KeyPressAction.UpdateStatus:
                    await UpdateDisplay(args);
                    break;

                case KeyPressAction.Run when pipelineType == PipelineType.Build:
                    await _service.StartBuild(SettingsModel);
                    break;

                case KeyPressAction.Run when pipelineType == PipelineType.Release:
                    await _service.StartRelease(SettingsModel);
                    break;
            }
        }
    }
}
