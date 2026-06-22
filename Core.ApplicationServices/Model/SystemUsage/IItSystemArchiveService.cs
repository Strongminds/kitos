using Core.Abstractions.Types;
using Core.DomainModel.Archive;
using System;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public interface IItSystemArchiveService
    {
        Result<ItSystemArchive, OperationError> Create(Guid systemUsageUuid, ArchiveItSystemUsageParameters parameters);
    }
}
