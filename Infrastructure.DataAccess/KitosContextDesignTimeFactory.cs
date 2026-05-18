using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DataAccess
{
    /// <summary>
    /// Used by 'dotnet ef' tooling at design time (migrations, scaffolding).
    ///
    /// Connection string resolution order:
    ///   1. Environment variable <c>ConnectionStrings__KitosContext</c>
    ///   2. <c>appsettings.json</c> found by walking up from the current directory
    ///      (picks up Presentation.Web/appsettings.json automatically)
    /// </summary>
    public class KitosContextDesignTimeFactory : IDesignTimeDbContextFactory<KitosContext>
    {
        private const string EnvVar = "ConnectionStrings__KitosContext";
        private const string ConnectionStringKey = "ConnectionStrings:KitosContext";
        private const string AppSettingsFile = "appsettings.json";

        public KitosContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable(EnvVar);

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = ReadFromAppSettings();

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Design-time DB context could not resolve a connection string. " +
                    $"Either set the '{EnvVar}' environment variable or ensure an '{AppSettingsFile}' " +
                    $"with a 'ConnectionStrings:KitosContext' entry is reachable from the working directory.");

            var optionsBuilder = new DbContextOptionsBuilder<KitosContext>();
            optionsBuilder.UseLazyLoadingProxies().UseSqlServer(connectionString);

            return new KitosContext(optionsBuilder.Options);
        }

        private static string? ReadFromAppSettings()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, AppSettingsFile);
                if (File.Exists(candidate))
                {
                    var config = new ConfigurationBuilder()
                        .SetBasePath(dir.FullName)
                        .AddJsonFile(AppSettingsFile, optional: false)
                        .Build();

                    var value = config[ConnectionStringKey];
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
                dir = dir.Parent;
            }
            return null;
        }
    }
}
