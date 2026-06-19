using System;

namespace Core.DomainModel.Archive;

public interface IItArchiveSnapshot : IHasUuid
{
    public Guid ItSystemArchiveUuid { get; set; }
    public ItSystemArchive ItSystemArchive { get; set; }
}
