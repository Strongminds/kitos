using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.DomainModel.Archive;
using System;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public interface IItSystemArchiveService
    {
        Result<ItSystemArchive, OperationError> Create(Guid systemUsageUuid, ArchiveItSystemUsageParameters parameters);
        Result<ItSystemArchive, OperationError> GetByUuid(Guid archiveUuid);
        Result<ItSystemArchive, OperationError> Delete(Guid archiveUuid);
        Result<ResourcePermissionsResult, OperationError> GetPermissions(Guid uuid);
        Result<ResourceCollectionPermissionsResult, OperationError> GetCollectionPermissions(Guid organizationUuid);
    }
}
