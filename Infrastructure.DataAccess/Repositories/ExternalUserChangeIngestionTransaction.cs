using System;
using System.Data;
using Core.Abstractions.Types;
using Core.DomainModel.Users;
using Core.DomainServices.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.DataAccess.Repositories;

public class ExternalUserChangeIngestionTransaction(KitosContext db) : IExternalUserChangeIngestionTransaction
{
    public Result<ExternalUserChange, OperationError> Execute(Func<Result<ExternalUserChange, OperationError>> ingestion)
    {
        using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var result = ingestion();
            if (result.Failed) return result;
            transaction.Commit();
            return result;
        }
        catch (Exception exception) when (IsConcurrentIngestion(exception))
        {
            transaction.Rollback();
            db.ChangeTracker.Clear();
            // The job retries with a fresh snapshot, then observes the winning event or resolved state.
            return new OperationError("Concurrent ingestion. Retry the event.", OperationFailure.UnknownError);
        }
    }

    private static bool IsConcurrentIngestion(Exception exception) => exception switch
    {
        PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.UniqueViolation } => true,
        DbUpdateException { InnerException: { } inner } => IsConcurrentIngestion(inner),
        _ => false
    };
}
