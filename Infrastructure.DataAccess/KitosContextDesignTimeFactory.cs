using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Infrastructure.DataAccess
{
    /// <summary>
    /// Used by 'dotnet ef' tooling at design time (migrations, scaffolding).
    /// The connection string is read from the environment variable
    /// <c>ConnectionStrings__KitosContext</c> (standard .NET hierarchical config format).
    ///
    /// Set it before running any 'dotnet ef' command, e.g.:
    ///   $env:ConnectionStrings__KitosContext = "Server=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Kitos;MultipleActiveResultSets=True;TrustServerCertificate=True"
    /// </summary>
    public class KitosContextDesignTimeFactory : IDesignTimeDbContextFactory<KitosContext>
    {
        private const string EnvVar = "ConnectionStrings__KitosContext";
        private const string ProviderEnvVar = "Database__Provider";

        public KitosContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable(EnvVar);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Design-time DB context requires the '{EnvVar}' environment variable to be set. " +
                    "Example: $env:ConnectionStrings__KitosContext = \"Server=.\\SQLEXPRESS;Integrated Security=true;Initial Catalog=Kitos;MultipleActiveResultSets=True;TrustServerCertificate=True\"");

            var provider = Environment.GetEnvironmentVariable(ProviderEnvVar);
            var optionsBuilder = new DbContextOptionsBuilder<KitosContext>();
            optionsBuilder.UseLazyLoadingProxies();

            if (IsPostgreSqlProvider(provider))
            {
                var pgCsb = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = "dbo,public" };
                optionsBuilder.UseNpgsql(pgCsb.ConnectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"));
            }
            else
            {
                optionsBuilder.UseSqlServer(connectionString);
            }

            return new KitosContext(optionsBuilder.Options);
        }

        private static bool IsPostgreSqlProvider(string? provider)
        {
            return string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase);
        }
    }
}
