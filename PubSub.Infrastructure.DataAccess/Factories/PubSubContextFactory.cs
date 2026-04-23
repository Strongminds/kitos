using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PubSub.Infrastructure.DataAccess.Factories
{
    public class PubSubContextFactory : IDesignTimeDbContextFactory<PubSubContext>
    {
        private const string ProviderEnvVar = "Database__Provider";

        public PubSubContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION_STRING");
            var provider = Environment.GetEnvironmentVariable(ProviderEnvVar);

            var optionsBuilder = new DbContextOptionsBuilder<PubSubContext>();

            if (IsPostgreSqlProvider(provider))
            {
                optionsBuilder.UseNpgsql(connectionString);
            }
            else
            {
                optionsBuilder.UseSqlServer(connectionString);
            }

            return new PubSubContext(optionsBuilder.Options);
        }

        private static bool IsPostgreSqlProvider(string? provider)
        {
            return string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase);
        }
    }
}
