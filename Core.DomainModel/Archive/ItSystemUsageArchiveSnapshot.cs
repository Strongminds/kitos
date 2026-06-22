using System;

namespace Core.DomainModel.Archive;

public class ItSystemUsageArchiveSnapshot: Entity, IArchiveSnapshotEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string LegacyName { get; set; }
    public string LocalName { get; set; }
    public string LocalId { get; set; }

    public Guid ItSystemUuid { get; set; }
    public ItSystem.ItSystem ItSystem { get; set; }

    public Guid ItSystemArchiveUuid { get; set; }
    public virtual ItSystemArchive ItSystemArchive { get; set; }
}
