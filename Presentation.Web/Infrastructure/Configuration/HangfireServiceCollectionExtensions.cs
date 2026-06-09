using Core.Abstractions.Helpers;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Server;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Presentation.Web.Hangfire;
using System;

namespace Presentation.Web.Infrastructure.Configuration
{
    public static class HangfireServiceCollectionExtensions
    {
        public static IServiceCollection AddKitosHangfire(this IServiceCollection services, IConfiguration configuration)
        {
            var hangfireConnectionString = configuration.GetConnectionString("kitos_HangfireDB")
                ?? throw new InvalidOperationException("kitos_HangfireDB connection string is required");
            var hangfireProvider = configuration["Hangfire:Provider"] ?? configuration["Database:Provider"];

            EnsureHangfireDatabaseCreated(hangfireConnectionString, hangfireProvider);

            services.AddHangfire(config =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings();

                if (DatabaseProviderHelper.IsPostgreSqlProvider(hangfireProvider))
                {
                    config.UsePostgreSqlStorage(hangfireConnectionString, new PostgreSqlStorageOptions
                    {
                        // Ensure Hangfire creates its own schema/tables in fresh PostgreSQL databases.
                        PrepareSchemaIfNecessary = true,
                        SchemaName = "hangfire"
                    });
                }
                else
                {
                    config.UseSqlServerStorage(hangfireConnectionString);
                }
            });

            services.AddSingleton<IBackgroundProcess>(provider => new KeepReadModelsInSyncProcess(provider));
            services.AddHangfireServer();

            return services;
        }

        private static void EnsureHangfireDatabaseCreated(string hangfireConnectionString, string? provider)
        {
            if (DatabaseProviderHelper.IsPostgreSqlProvider(provider))
            {
                var csb = new NpgsqlConnectionStringBuilder(hangfireConnectionString);
                var databaseName = csb.Database;
                if (string.IsNullOrWhiteSpace(databaseName))
                    throw new InvalidOperationException("Hangfire PostgreSQL connection string must include a database name.");

                csb.Database = "postgres";

                using var connection = new NpgsqlConnection(csb.ConnectionString);
                connection.Open();
                using var existsCmd = connection.CreateCommand();
                existsCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName";
                existsCmd.Parameters.AddWithValue("dbName", databaseName);

                var exists = existsCmd.ExecuteScalar() != null;
                if (!exists)
                {
                    using var createCmd = connection.CreateCommand();
                    createCmd.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
                    createCmd.ExecuteNonQuery();
                }

                // Hangfire.PostgreSql probes "hangfire"."schema" very early; on some fresh
                // databases this can happen before its auto-preparation step. Ensure the
                // bootstrap objects exist so startup is deterministic.
                csb.Database = databaseName;
                using var hangfireConnection = new NpgsqlConnection(csb.ConnectionString);
                hangfireConnection.Open();
                using var bootstrapCmd = hangfireConnection.CreateCommand();
                bootstrapCmd.CommandText = """
                    CREATE SCHEMA IF NOT EXISTS hangfire;
                    CREATE TABLE IF NOT EXISTS hangfire."schema" (
                        "version" integer NOT NULL
                    );
                    """;
                bootstrapCmd.ExecuteNonQuery();

                return;
            }

            var sqlCsb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(hangfireConnectionString);
            var sqlDatabaseName = sqlCsb.InitialCatalog;
            sqlCsb.InitialCatalog = "master";

            using var sqlConnection = new Microsoft.Data.SqlClient.SqlConnection(sqlCsb.ConnectionString);
            sqlConnection.Open();
            using var cmd = sqlConnection.CreateCommand();
            cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{sqlDatabaseName}') CREATE DATABASE [{sqlDatabaseName}]";
            cmd.ExecuteNonQuery();
        }

    }
}
