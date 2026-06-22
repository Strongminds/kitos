namespace Presentation.Web.Models.API.V2.Request.SystemUsage
{
    public class ArchiveReferenceDTO
    {
        /// <summary>
        /// Label for the archive reference
        /// </summary>
        public required string Label { get; set; }

        /// <summary>
        /// URL for the archive reference
        /// </summary>
        public required string Url { get; set; }
    }
}
