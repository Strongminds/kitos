using Core.Abstractions.Types;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices.Generic;
using Core.DomainServices.Repositories.SystemUsage;
using System;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public class ItSystemUsageArchiveService(IItSystemUsageRepository itSystemUsageRepository, IEntityIdentityResolver entityIdentityResolver) : IItSystemUsageArchiveService
    {
        public Result<ItSystemUsage, OperationError> Create(Guid systemUsageUuid)
        {
            var systemUsageId = entityIdentityResolver.ResolveDbId<ItSystemUsage>(systemUsageUuid);
            if (systemUsageId.IsNone) return new OperationError($"Id for system usage with uuid was not found: {systemUsageUuid}", OperationFailure.NotFound);
            var systemUsage = itSystemUsageRepository.GetSystemUsage(systemUsageId.Value);
            if (systemUsage == null) return new OperationError($"System usage with uuid was not found: {systemUsageUuid}", OperationFailure.NotFound);
            return new OperationError("not implemented", OperationFailure.UnknownError);
        }
    }
}
