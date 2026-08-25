using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Server;
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

            EnsureHangfireDatabaseCreated(hangfireConnectionString);

            services.AddHangfire(config =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(hangfireConnectionString), new PostgreSqlStorageOptions
                    {
                        PrepareSchemaIfNecessary = true,
                        SchemaName = "hangfire"
                    });
            });

            services.AddSingleton<IBackgroundProcess>(provider => new KeepReadModelsInSyncProcess(provider));
            services.AddHangfireServer();

            return services;
        }

        private static void EnsureHangfireDatabaseCreated(string hangfireConnectionString)
        {
            var csb = new NpgsqlConnectionStringBuilder(hangfireConnectionString);
            if (string.IsNullOrWhiteSpace(csb.Database))
                throw new InvalidOperationException("Hangfire PostgreSQL connection string must include a database name.");

            // The Hangfire database is created by the deployment setup script before the app starts.
            // Connecting to the postgres maintenance DB as the app user is not supported in all
            // environments (the app user may only have access to its own databases).
            // Just ensure the hangfire schema exists; Hangfire.PostgreSql will create its own tables.
            using var hangfireConnection = new NpgsqlConnection(hangfireConnectionString);
            hangfireConnection.Open();
            using var bootstrapCmd = hangfireConnection.CreateCommand();
            bootstrapCmd.CommandText = "CREATE SCHEMA IF NOT EXISTS hangfire;";
            bootstrapCmd.ExecuteNonQuery();
        }

    }
}
