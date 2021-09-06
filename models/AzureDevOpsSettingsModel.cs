namespace StreamDeckAzureDevOps.Models
{
    public class AzureDevOpsSettingsModel
    {
        public string ProjectName { get; set; } = "";
        public string OrganizationName { get; set; } = "";
        public string PAT { get; set; } = "";
        
        /// <summary>
        /// Set as integer because enum <see cref="PipelineType"/> is not working with JS.
        /// </summary>
        public int PipelineType { get; set; } = 0;
        public int DefinitionId { get; set; } = 0;
    }

    public enum PipelineType
    {
        Build = 0,
        Release = 1
    }
}
