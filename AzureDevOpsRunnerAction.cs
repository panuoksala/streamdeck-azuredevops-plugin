using Microsoft.Extensions.Logging;
using Serilog;
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
        private readonly AzureDevOpsService _service;

        public AzureDevOpsRunnerAction()
        {
            _service = new AzureDevOpsService(Logger);
        }

        public async Task UpdateStatus(string context)
        {
            await Manager.SetImageAsync(context, "images/Azure-DevOps-updating.png");

            string statusImage = (PipelineType)SettingsModel.PipelineType switch
            {
                PipelineType.Build => await _service.GetBuildStatusImage(SettingsModel),
                PipelineType.Release => await _service.GetReleaseStatusImage(SettingsModel),
                _ => throw new ArgumentOutOfRangeException($"Unsupported pipeline type {SettingsModel.PipelineType}."),
            };

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
                && !string.IsNullOrWhiteSpace(SettingsModel.OrganizationURL)
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
                    if ((StatusUpdateFrequency)SettingsModel.UpdateStatusEverySecond == StatusUpdateFrequency.Never)
                    {
                        await Manager.SetImageAsync(args.context, "images/Azure-DevOps-success.png");
                    }
                    else
                    {
                        await Manager.SetImageAsync(args.context, "images/Azure-DevOps-waiting.png");
                    }
                    break;
            }

            SettingsModel.ErrorMessage = string.Empty;
            await Manager.SetSettingsAsync(args.context, SettingsModel);
        }
    }
}
