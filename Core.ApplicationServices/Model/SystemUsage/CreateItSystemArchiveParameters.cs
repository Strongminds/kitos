using System;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public class CreateItSystemArchiveParameters
    {
        public required DateTime ArchivingDate { get; set; }
        public required string ReferenceName { get; set; }
        public required string Note { get; set; }
    }
}
