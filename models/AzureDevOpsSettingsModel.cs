using System;

namespace StreamDeckAzureDevOps.Models
{
    public class AzureDevOpsSettingsModel
    {
        /// <summary>
        /// The Azure DevOps project name
        /// </summary>
        public string ProjectName { get; set; } = "";

        /// <summary>
        /// <see cref="OrganizationNameFormatted"/> for handling different url formates
        /// </summary>
        public string OrganizationURL { get; set; } = "";

        /// <summary>
        /// PAT token created at Azure DevOps
        /// </summary>
        public string PAT { get; set; } = "";
        
        /// <summary>
        /// Set as integer because enum <see cref="PipelineType"/> is not working with JS.
        /// </summary>
        public int PipelineType { get; set; } = 0;

        /// <summary>
        /// Build or release definition unique identifier. Can be picked from Azure DevOps build/release pipeline URL
        /// </summary>
        public int DefinitionId { get; set; } = 0;
        
        /// <summary>
        /// The branch name
        /// </summary>
        public string BranchName { get; set; } = "";

        /// <summary>
        /// What action occures after single tap on StreamDeck button
        /// </summary>
        public int TapAction { get; set; } = 1;
        
        /// <summary>
        /// What action occures after long press on button.
        /// </summary>
        public int LongPressAction { get; set; } = 2;

        /// <summary>
        /// How often build/release status is fethced from Azure DevOps
        /// </summary>
        public int UpdateStatusEverySecond { get; set; } = 0;

        /// <summary>
        /// Possible error message that occures
        /// </summary>
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

        public string GetFullBranchName() => !string.IsNullOrWhiteSpace(BranchName) ? $"refs/heads/{BranchName}" : null;

        public string OrganizationNameFormatted() => OrganizationURL.Contains("https://") ? OrganizationURL : $"https://{OrganizationURL}";        
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
        Run = 2,
        Open = 3
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
