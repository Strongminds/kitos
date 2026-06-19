using System;

namespace Core.DomainModel.Archive;

public interface IArchiveSnapshotEntity : IHasUuid
{
    Guid ItSystemArchiveUuid { get; set; }
    ItSystemArchive ItSystemArchive { get; set; }
}
