using System;
using System.Collections.Generic;

namespace Core.DomainModel.Archive;

public class ItSystemArchive: Entity, IArchiveEntity<ItSystemUsageArchiveSnapshot>
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public Guid SnapshotUuid { get; set; }
    public virtual ItSystemUsageArchiveSnapshot Snapshot { get; set; }

    public Guid OrganizationUuid { get; set; }
    public virtual Organization.Organization Organization { get; set; }

    public required string Note { get; set; }
    public required DateTime ArchivingDate { get; set; }
    public string ReferenceName { get; set; }

    public virtual ICollection<ArchiveReference> ArchiveReferences { get; set; }
}
