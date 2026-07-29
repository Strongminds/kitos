using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Tests.Infrastructure.Helpers;

namespace Tests.Infrastructure.Tests;

[Collection(MigrationTestsCollection.Name)]
public sealed class SqlServerMigrationTest : IAsyncLifetime
{
    private const string DatabaseName = "kitos";
    private const string HangfireDatabaseName = "hangfire";
    private const string UserName = "sa";
    private const string Password = "StrongPassword123!";

    private readonly IContainer _container = new ContainerBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_SA_PASSWORD", Password)
        .WithPortBinding(1433, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1433))
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task Can_Run_Migrations_Against_SqlServer()
    {
        var connectionString =
            $"Server={_container.Hostname},{_container.GetMappedPublicPort(1433)};Database={DatabaseName};User ID={UserName};Password={Password};TrustServerCertificate=True;Encrypt=False";
        var hangfireConnectionString =
            $"Server={_container.Hostname},{_container.GetMappedPublicPort(1433)};Database={HangfireDatabaseName};User ID={UserName};Password={Password};TrustServerCertificate=True;Encrypt=False";

        await MigrationTestHelper.AssertNoPendingMigrationsAsync("SqlServer", connectionString, hangfireConnectionString);
    }
}
