using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        public KitosContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable(EnvVar);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Design-time DB context requires the '{EnvVar}' environment variable to be set. " +
                    "Example: $env:ConnectionStrings__KitosContext = \"Server=.\\SQLEXPRESS;Integrated Security=true;Initial Catalog=Kitos;MultipleActiveResultSets=True;TrustServerCertificate=True\"");

            var optionsBuilder = new DbContextOptionsBuilder<KitosContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new KitosContext(optionsBuilder.Options);
        }
    }
}
