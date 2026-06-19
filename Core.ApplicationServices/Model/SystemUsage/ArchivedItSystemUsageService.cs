using Core.Abstractions.Types;
using Core.DomainModel.ItSystemUsage;
using System;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public class ArchivedItSystemUsageService : IArchivedItSystemUsageService
    {
        public Result<ItSystemUsage, OperationError> Create(Guid systemUsageUuid)
        {
            throw new NotImplementedException();
        }
    }
}
