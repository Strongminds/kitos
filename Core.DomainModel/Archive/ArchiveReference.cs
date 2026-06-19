using System;

namespace Core.DomainModel.Archive;

public class ArchiveReference: IHasUuid
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string Label { get; set; }
    public string Url { get; set; }

    public Guid ItSystemArchiveUuid { get; set; }
    public ItSystemArchive ItSystemArchive { get; set; }
}
