using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Tests.Container.Tests;

[Collection(MigrationTestsCollection.Name)]
public sealed class PostgreSqlTransactionTest : IAsyncLifetime
{
    private const string DatabaseName = "transaction_tests";
    private const string UserName = "postgres";
    private const string Password = "postgres";
    private readonly IContainer _postgres = new ContainerBuilder("postgres:16-alpine")
        .WithEnvironment("POSTGRES_DB", DatabaseName)
        .WithEnvironment("POSTGRES_USER", UserName)
        .WithEnvironment("POSTGRES_PASSWORD", Password)
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Theory]
    [InlineData("commit")]
    [InlineData("rollback")]
    [InlineData("dispose")]
    public void AfterCommit_NestedTransactions_ReflectsActualDatabaseOutcome(string outcome)
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = _postgres.Hostname,
            Port = _postgres.GetMappedPublicPort(5432),
            Database = DatabaseName,
            Username = UserName,
            Password = Password
        }.ConnectionString;
        using var observer = new NpgsqlConnection(connectionString);
        observer.Open();
        using (var command = new NpgsqlCommand(
                   "CREATE TABLE IF NOT EXISTS callback_test (id uuid PRIMARY KEY)", observer))
            command.ExecuteNonQuery();
        var id = Guid.NewGuid();
        using var context = new KitosContext(new DbContextOptionsBuilder<KitosContext>()
            .UseNpgsql(connectionString).Options);
        using var manager = new TransactionManager(context);
        var calls = 0;
        using (var root = manager.Begin())
        {
            context.Database.ExecuteSqlInterpolated($"INSERT INTO callback_test (id) VALUES ({id})");
            using (var archive = manager.Begin())
            {
                using (var deletion = manager.Begin())
                {
                    deletion.AfterCommit(() =>
                    {
                        using var command = new NpgsqlCommand("SELECT COUNT(*) FROM callback_test WHERE id = @id", observer);
                        command.Parameters.AddWithValue("id", id);
                        Assert.Equal(1L, command.ExecuteScalar());
                        calls++;
                    });
                    deletion.Commit();
                }
                archive.Commit();
            }
            Assert.Equal(0, calls);
            if (outcome == "commit")
                root.Commit();
            else if (outcome == "rollback")
                root.Rollback();
        }

        Assert.Equal(outcome == "commit" ? 1 : 0, calls);
        using (var next = manager.Begin())
        {
            next.AfterCommit(() => calls++);
            next.Commit();
        }
        Assert.Equal(outcome == "commit" ? 2 : 1, calls);
    }
}
