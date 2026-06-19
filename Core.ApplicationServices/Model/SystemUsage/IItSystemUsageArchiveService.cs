using Core.Abstractions.Types;
using Core.DomainModel.ItSystemUsage;
using System;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public interface IItSystemUsageArchiveService
    {
        Result<ItSystemUsage, OperationError> Create(Guid systemUsageUuid);
    }
}
