using System;

namespace Presentation.Web.Models.API.V2.Response.SystemUsage
{
    public class ArchiveReferenceResponseDTO
    {
        public Guid Uuid { get; set; }
        public required string Label { get; set; }
        public required string Url { get; set; }
    }
}
