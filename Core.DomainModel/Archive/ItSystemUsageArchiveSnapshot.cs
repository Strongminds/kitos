using System;

namespace Core.DomainModel.Archive;

public class ItSystemUsageArchiveSnapshot: IHasUuid
{
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string LegacyName { get; set; }
    public string LocalName { get; set; }
    public string LocalId { get; set; }
    
    public Guid ItSystemArchiveUuid { get; set; }
    public ItSystemArchive ItSystemArchive { get; set; }
}
