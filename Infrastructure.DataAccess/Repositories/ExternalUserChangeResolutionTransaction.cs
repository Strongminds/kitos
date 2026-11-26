using System;
using System.Data;
using Core.Abstractions.Types;
using Core.DomainModel.Users;
using Core.DomainServices.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.DataAccess.Repositories;

public class ExternalUserChangeResolutionTransaction(KitosContext db) : IExternalUserChangeResolutionTransaction
{
    public void ClearTracking() => db.ChangeTracker.Clear();

    public Result<ExternalUserChange, OperationError> Execute(Func<Result<ExternalUserChange, OperationError>> resolution)
    {
        using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var result = resolution();
            if (result.Failed) return result;
            transaction.Commit();
            return result;
        }
        catch (Exception ex) when (IsConcurrentResolution(ex))
        {
            transaction.Rollback();
            db.ChangeTracker.Clear();
            return new OperationError("Concurrent resolution. Reload the change before retrying.", OperationFailure.Conflict);
        }
    }

    private static bool IsConcurrentResolution(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
            if (current is DbUpdateConcurrencyException ||
                current is PostgresException { SqlState: "40001" or "23505" })
                return true;
        return false;
    }
}
