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

                if (IsPostgreSqlProvider(hangfireProvider))
                {
                    config.UsePostgreSqlStorage(hangfireConnectionString);
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
            if (IsPostgreSqlProvider(provider))
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

        private static bool IsPostgreSqlProvider(string? provider)
        {
            return string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase);
        }
    }
}
