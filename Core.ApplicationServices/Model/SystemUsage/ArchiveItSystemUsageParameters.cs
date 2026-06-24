using Core.ApplicationServices.Model.Archive;
using System;
using System.Collections.Generic;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public class ArchiveItSystemUsageParameters
    {
        public required DateTime ArchivingDate { get; set; }
        public string? ReferenceName { get; set; }
        public required string Note { get; set; }
        public IEnumerable<ArchiveReferenceProperties> ArchiveReferences { get; set; } = new List<ArchiveReferenceProperties>();
    }
}
