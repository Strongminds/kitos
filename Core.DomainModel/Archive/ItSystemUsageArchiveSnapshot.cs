using System;

namespace Core.DomainModel.Archive;

// TODO add IItArchiveSnapshot interface
public class ItSystemUsageArchiveSnapshot: Entity, IItArchiveSnapshot
{
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string LegacyName { get; set; }
    public string LocalName { get; set; }
    public string LocalId { get; set; }
    
    public Guid ItSystemArchiveUuid { get; set; }
    public virtual ItSystemArchive ItSystemArchive { get; set; }
}
