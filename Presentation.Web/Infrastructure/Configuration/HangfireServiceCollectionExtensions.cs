using Hangfire;
using Hangfire.Server;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(hangfireConnectionString));

            services.AddSingleton<IBackgroundProcess>(provider => new KeepReadModelsInSyncProcess(provider));
            services.AddHangfireServer();

            return services;
        }

        private static void EnsureHangfireDatabaseCreated(string hangfireConnectionString)
        {
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(hangfireConnectionString);
            var databaseName = csb.InitialCatalog;
            csb.InitialCatalog = "master";

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}') CREATE DATABASE [{databaseName}]";
            cmd.ExecuteNonQuery();
        }
    }
}
