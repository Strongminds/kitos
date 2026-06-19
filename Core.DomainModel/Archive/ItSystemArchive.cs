using System;
using System.Collections.Generic;

namespace Core.DomainModel.Archive;

public class ItSystemArchive: IHasUuid
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public Guid ItSystemUsageSnapshotUuid { get; set; }
    public ItSystemUsageArchiveSnapshot ItSystemUsageArchiveSnapshot { get; set; }

    public Guid OrganizationUuid { get; set; }
    public Organization.Organization Organization { get; set; }

    public required string Note { get; set; }
    public required DateTime ArchivingDate { get; set; }
    public required string ESDHName { get; set; }
    public ICollection<ArchiveReference> ArchiveReferences { get; set; }
}
