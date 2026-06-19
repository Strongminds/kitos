using System;
using System.Collections.Generic;

namespace Core.DomainModel.Archive;

public class ItSystemArchive: Entity, IItArchive
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public Guid ItSystemUsageSnapshotUuid { get; set; }
    public virtual ItSystemUsageArchiveSnapshot ItSystemUsageArchiveSnapshot { get; set; }

    public Guid OrganizationUuid { get; set; }
    public virtual Organization.Organization Organization { get; set; }

    public required string Note { get; set; }
    public required DateTime ArchivingDate { get; set; }
    public required string ReferenceName { get; set; }
    public virtual ICollection<ArchiveReference> ArchiveReferences { get; set; }
}
