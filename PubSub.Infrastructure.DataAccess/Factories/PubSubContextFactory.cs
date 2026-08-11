using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using PubSub.Core.Abstractions.Helpers;

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

            var isPostgreSql = DatabaseProviderHelper.IsPostgreSqlProvider(provider)
                               || DatabaseProviderHelper.LooksLikePostgreSqlConnectionString(connectionString);

            if (isPostgreSql)
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                dataSourceBuilder.ConnectionStringBuilder.GssEncryptionMode = GssEncryptionMode.Disable;
                optionsBuilder.UseNpgsql(dataSourceBuilder.Build());
            }
            else
            {
                optionsBuilder.UseSqlServer(connectionString);
            }

            return new PubSubContext(optionsBuilder.Options);
        }

    }
}
