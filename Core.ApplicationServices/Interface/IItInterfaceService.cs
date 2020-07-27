﻿using Core.DomainModel.ItSystem;
using Core.DomainModel.Result;

namespace Core.ApplicationServices.Interface
{
    public interface IItInterfaceService
    {
        Result<ItInterface, OperationFailure> Delete(int id);
        Result<ItInterface, OperationFailure> Create(int organizationId, string name, string interfaceId);
        Result<ItInterface, OperationFailure> ChangeExposingSystem(int interfaceId, int? newSystemId);
    }
}
