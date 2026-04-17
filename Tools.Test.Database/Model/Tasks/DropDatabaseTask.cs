using System;
using System.Threading;
using Microsoft.Data.SqlClient;
using Infrastructure.DataAccess;

namespace Tools.Test.Database.Model.Tasks
{
    public class DropDatabaseTask : DatabaseTask
    {
        private const int MaxAttempts = 5;
        private const int RetryDelayMs = 3000;

        private readonly string _connectionString;

        public DropDatabaseTask(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override bool Execute(KitosContext context)
        {
            //Create a new connection string without initial catalog so that db can be dropped
            var connectionStringBuilder = new SqlConnectionStringBuilder(_connectionString);
            var dbName = connectionStringBuilder.InitialCatalog;
            connectionStringBuilder.Remove("Initial Catalog");
            // Disable connection pooling so each attempt gets a fresh physical connection
            connectionStringBuilder.Pooling = false;

            var sqlToDropDb =
                $"IF DB_ID('{dbName}') IS NOT NULL " +
                "BEGIN " +
                    $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                    $"DROP DATABASE [{dbName}]; " +
                "END ";

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var connection = new SqlConnection(connectionStringBuilder.ConnectionString);
                    connection.Open();
                    using var sqlCommand = connection.CreateCommand();
                    sqlCommand.CommandText = sqlToDropDb;
                    sqlCommand.ExecuteNonQuery();
                    return true;
                }
                catch (SqlException ex) when (attempt < MaxAttempts)
                {
                    Console.WriteLine($"Attempt {attempt}/{MaxAttempts} failed (SQL error {ex.Number}): {ex.Message}. Retrying in {RetryDelayMs}ms...");
                    Thread.Sleep(RetryDelayMs);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return true;
        }
    }
}
