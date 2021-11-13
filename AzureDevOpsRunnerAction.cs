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
                    statusImage = await _service.GetReleaseStatusImage(SettingsModel);
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
            SettingsModel.ErrorMessage = ex.Message;

            await Manager.ShowAlertAsync(args.context);
            await Manager.SetImageAsync(args.context, "images/Azure-DevOps-unknown.png");

            await Manager.SetSettingsAsync(args.context, SettingsModel);
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
                    // Do nothing :)
                    break;

                case KeyPressAction.UpdateStatus:
                    await UpdateDisplay(args);
                    await Manager.ShowOkAsync(args.context);
                    break;

                case KeyPressAction.Run:
                    await Manager.SetImageAsync(args.context, "images/Azure-DevOps-updating.png");

                    if (pipelineType == PipelineType.Build)
                    {
                        await _service.StartBuild(SettingsModel);
                    }
                    else
                    {
                        await _service.StartRelease(SettingsModel);
                    }

                    await Manager.ShowOkAsync(args.context);

                    await UpdateDisplay(args);
                    break;
            }

            SettingsModel.ErrorMessage = string.Empty;
            await Manager.SetSettingsAsync(args.context, SettingsModel);
        }
    }
}
