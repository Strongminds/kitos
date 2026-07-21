using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Tests.Infrastructure;

public sealed class PostgreSqlMigrationTest : IAsyncLifetime
{
    private const string DatabaseName = "kitos";
    private const string UserName = "postgres";
    private const string Password = "postgres";
    private const string ConnectionStringEnvVar = "ConnectionStrings__KitosContext";
    private const string ProviderEnvVar = "Database__Provider";
    private const string IgnorePendingModelChangesWarningEnvVar = "IgnorePendingModelChangesWarning";

    private readonly IContainer _container = new ContainerBuilder("postgres:16-alpine")
        .WithEnvironment("POSTGRES_DB", DatabaseName)
        .WithEnvironment("POSTGRES_USER", UserName)
        .WithEnvironment("POSTGRES_PASSWORD", Password)
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
        .Build();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Can_Run_Migrations_Against_Postgresql()
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        var previousProvider = Environment.GetEnvironmentVariable(ProviderEnvVar);
        var previousIgnorePendingModelChangesWarning = Environment.GetEnvironmentVariable(IgnorePendingModelChangesWarningEnvVar);

        try
        {
            var connectionString =
                $"Host={_container.Hostname};Port={_container.GetMappedPublicPort(5432)};Database={DatabaseName};Username={UserName};Password={Password}";
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, connectionString);
            Environment.SetEnvironmentVariable(ProviderEnvVar, "postgresql");
            Environment.SetEnvironmentVariable(IgnorePendingModelChangesWarningEnvVar, "true");

            var factory = new KitosContextDesignTimeFactory();
            await using var context = factory.CreateDbContext([]);

            await context.Database.MigrateAsync();

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            Assert.Empty(pendingMigrations);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, previousConnectionString);
            Environment.SetEnvironmentVariable(ProviderEnvVar, previousProvider);
            Environment.SetEnvironmentVariable(IgnorePendingModelChangesWarningEnvVar, previousIgnorePendingModelChangesWarning);
        }
    }

}
