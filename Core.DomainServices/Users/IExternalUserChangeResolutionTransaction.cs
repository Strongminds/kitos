using System;
using Core.Abstractions.Types;
using Core.DomainModel.Users;

namespace Core.DomainServices.Users;

public interface IExternalUserChangeResolutionTransaction
{
    void ClearTracking();

    Result<ExternalUserChange, OperationError> Execute(Func<Result<ExternalUserChange, OperationError>> resolution);
}
