using System;
using Presentation.Web.Models.API.V2.Response.Organization;

namespace Presentation.Web.Models.API.V2.Response.SystemUsage
{
    public class ItSystemArchiveResponseDTO
    {
        /// <summary>
        /// Unique identifier for the archive
        /// </summary>
        public required Guid Uuid { get; set; }

        /// <summary>
        /// The date when the archive was created
        /// </summary>
        public required DateTime ArchivingDate { get; set; }

        /// <summary>
        /// A reference name for the archive
        /// </summary>
        public required string ReferenceName { get; set; }

        /// <summary>
        /// Notes about the archive
        /// </summary>
        public required string Note { get; set; }

        /// <summary>
        /// The organization in which the archive was created
        /// </summary>
        public ShallowOrganizationResponseDTO? Organization { get; set; }
    }
}
