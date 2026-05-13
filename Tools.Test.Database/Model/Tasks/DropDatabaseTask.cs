using System;
using System.Threading;
using Microsoft.Data.SqlClient;
using Infrastructure.DataAccess;
using Npgsql;

namespace Tools.Test.Database.Model.Tasks
{
    public class DropDatabaseTask : DatabaseTask
    {
        private readonly string _connectionString;
        private readonly string _provider;

        public DropDatabaseTask(string connectionString, string provider)
        {
            _connectionString = connectionString;
            _provider = provider;
        }

        public override bool Execute(KitosContext context)
        {
            return IsPostgreSqlProvider(_provider) ? DropPostgreSqlDatabase() : DropSqlServerDatabase();
        }

        private bool DropSqlServerDatabase()
        {
            // Connect to master so we can safely alter/drop the target database.
            var connectionStringBuilder = new SqlConnectionStringBuilder(_connectionString);
            var dbName = connectionStringBuilder.InitialCatalog;
            connectionStringBuilder.InitialCatalog = "master";

            using var connection = new SqlConnection(connectionStringBuilder.ConnectionString);
            try
            {
                connection.Open();

                const int maxAttempts = 15;
                const int delayMs = 5000;

                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        using var sqlCommand = connection.CreateCommand();
                        sqlCommand.CommandTimeout = 60;
                        sqlCommand.CommandText =
                            "DECLARE @dbName sysname = @name; " +
                            "IF DB_ID(@dbName) IS NOT NULL " +
                            "BEGIN " +
                                "DECLARE @killSql nvarchar(max) = N''; " +
                                "SELECT @killSql = @killSql + N'KILL ' + CONVERT(nvarchar(20), s.session_id) + N';' " +
                                "FROM sys.dm_exec_sessions s " +
                                "WHERE s.database_id = DB_ID(@dbName) AND s.session_id <> @@SPID; " +
                                "IF LEN(@killSql) > 0 EXEC(@killSql); " +
                                "DECLARE @quotedDbName nvarchar(258) = QUOTENAME(@dbName); " +
                                "DECLARE @sql nvarchar(max) = N'ALTER DATABASE ' + @quotedDbName + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;'; " +
                                "EXEC(@sql); " +
                                "SET @sql = N'DROP DATABASE ' + @quotedDbName + N';'; " +
                                "EXEC(@sql); " +
                            "END";
                        sqlCommand.Parameters.AddWithValue("@name", dbName);
                        sqlCommand.ExecuteNonQuery();

                        Console.WriteLine($"Dropped database '{dbName}'");
                        return true;
                    }
                    catch (SqlException sqlException) when ((sqlException.Number == 5061 || sqlException.Number == 3702) && attempt < maxAttempts)
                    {
                        Console.WriteLine(
                            $"Attempt {attempt}/{maxAttempts} failed (SQL error {sqlException.Number}): {sqlException.Message}. Retrying in {delayMs}ms...");
                        Thread.Sleep(delayMs);
                    }
                }

                throw new InvalidOperationException($"Failed to drop database '{dbName}' after {maxAttempts} attempts.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                connection.Close();
            }
        }

        private bool DropPostgreSqlDatabase()
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_connectionString);
            var dbName = connectionStringBuilder.Database;
            if (string.IsNullOrWhiteSpace(dbName))
            {
                throw new InvalidOperationException("PostgreSQL connection string does not include a database name.");
            }

            connectionStringBuilder.Database = "postgres";

            using var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
            try
            {
                connection.Open();

                const int maxAttempts = 15;
                const int delayMs = 5000;

                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        var escapedDbName = dbName.Replace("\"", "\"\"");

                        using var terminateConnectionsCommand = connection.CreateCommand();
                        terminateConnectionsCommand.CommandTimeout = 60;
                        terminateConnectionsCommand.CommandText =
                            "SELECT pg_terminate_backend(pid) " +
                            "FROM pg_stat_activity " +
                            "WHERE datname = @dbName " +
                            "AND pid <> pg_backend_pid();";
                        terminateConnectionsCommand.Parameters.AddWithValue("dbName", dbName);
                        terminateConnectionsCommand.ExecuteNonQuery();

                        using var dropDatabaseCommand = connection.CreateCommand();
                        dropDatabaseCommand.CommandTimeout = 60;
                        dropDatabaseCommand.CommandText = $"DROP DATABASE IF EXISTS \"{escapedDbName}\";";
                        dropDatabaseCommand.ExecuteNonQuery();

                        Console.WriteLine($"Dropped database '{dbName}'");
                        return true;
                    }
                    catch (PostgresException postgresException)
                        when ((postgresException.SqlState == "55006" || postgresException.SqlState == "57P04") && attempt < maxAttempts)
                    {
                        Console.WriteLine(
                            $"Attempt {attempt}/{maxAttempts} failed (PostgreSQL state {postgresException.SqlState}): {postgresException.MessageText}. Retrying in {delayMs}ms...");
                        Thread.Sleep(delayMs);
                    }
                }

                throw new InvalidOperationException($"Failed to drop database '{dbName}' after {maxAttempts} attempts.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                connection.Close();
            }
        }

        private static bool IsPostgreSqlProvider(string provider)
        {
            return string.IsNullOrWhiteSpace(provider) == false
                   && (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
                       || provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
                       || provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase));
        }
    }
}
