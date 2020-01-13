﻿using Core.ApplicationServices.Model.Result;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices.Model.Result;

namespace Core.ApplicationServices.Interface.Usage
{
    public interface IInterfaceUsageService
    {
        TwoTrackResult<ItInterfaceUsage, OperationFailure> Create(
            int systemUsageId,
            int systemId,
            int interfaceId,
            bool isWishedFor,
            int contractId,
            int? infrastructureId = null);

        TwoTrackResult<ItInterfaceUsage, OperationFailure> Delete(int systemUsageId, int systemId, int interfaceId);
    }
}
