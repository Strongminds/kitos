using System;
using Core.Abstractions.Types;
using Core.DomainModel.Users;

namespace Core.DomainServices.Users;

public interface IExternalUserChangeIngestionTransaction
{
    Result<ExternalUserChange, OperationError> Execute(Func<Result<ExternalUserChange, OperationError>> ingestion);
}
