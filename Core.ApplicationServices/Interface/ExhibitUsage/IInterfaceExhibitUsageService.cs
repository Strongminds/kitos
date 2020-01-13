﻿using Core.ApplicationServices.Model.Result;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices.Model.Result;

namespace Core.ApplicationServices.Interface.ExhibitUsage
{
    public interface IInterfaceExhibitUsageService
    {
        TwoTrackResult<ItInterfaceExhibitUsage, OperationFailure> Delete(int systemUsageId, int interfaceExhibitId);
    }
}
