using System;

namespace StreamDeckAzureDevOps.Models
{
    public class AzureDevOpsSettingsModel
    {
        public string BaseUrl { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string OrganizationName { get; set; } = "";
        public string PAT { get; set; } = "";
        
        /// <summary>
        /// Set as integer because enum <see cref="PipelineType"/> is not working with JS.
        /// </summary>
        public int PipelineType { get; set; } = 0;
        public int DefinitionId { get; set; } = 0;

        public int TapAction { get; set; } = 1;
        public int LongPressAction { get; set; } = 2;

        public int UpdateStatusEverySecond { get; set; } = 0;

        public string ErrorMessage { get; set; }

        public int GetUpdateFrequencyInSeconds()
        {
            return (StatusUpdateFrequency)UpdateStatusEverySecond switch
            {
                StatusUpdateFrequency.Never => throw new ArgumentOutOfRangeException("Cannot convert never into seconds."),
                StatusUpdateFrequency.Every10seconds => 10,
                StatusUpdateFrequency.Every30seconds => 30,
                StatusUpdateFrequency.Every60seconds => 60,
                StatusUpdateFrequency.Every180seconds => 180,
                StatusUpdateFrequency.Every300second => 300,
                _ => 180,
            };
        }
    }

    public enum PipelineType
    {
        Build = 0,
        Release = 1
    }

    public enum KeyPressAction
    {
        DoNothing = 0,
        UpdateStatus = 1,
        Run = 2
    }

    public enum StatusUpdateFrequency
    {
        Never = 0,
        Every10seconds = 1,
        Every30seconds = 2,
        Every60seconds = 3,
        Every180seconds = 4,
        Every300second = 5,
    }
}
