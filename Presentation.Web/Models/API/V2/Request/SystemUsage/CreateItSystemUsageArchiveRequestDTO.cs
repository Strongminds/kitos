using System;
using System.Collections.Generic;
using Presentation.Web.Models.API.V2.Types.Shared;

namespace Presentation.Web.Models.API.V2.Request.SystemUsage
{
    public class CreateItSystemUsageArchiveRequestDTO
    {
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
        /// References associated with the archive
        /// </summary>
        public IEnumerable<SimpleLinkDTO> ArchiveReferences { get; set; } = new List<SimpleLinkDTO>();
    }
}
