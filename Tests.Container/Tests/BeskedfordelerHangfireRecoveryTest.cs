using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.States;

namespace Tests.Container.Tests;

[Collection(MigrationTestsCollection.Name)]
public sealed class BeskedfordelerHangfireRecoveryTest : IAsyncLifetime
{
    private readonly IContainer _database = new ContainerBuilder("postgres:16-alpine")
        .WithEnvironment("POSTGRES_PASSWORD", "test")
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432)).Build();

    public Task InitializeAsync() => _database.StartAsync();
    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    [Fact]
    public async Task Enqueue_RestartFailureAndReplay_RetainsOriginalPayload()
    {
        var connection = $"Host={_database.Hostname};Port={_database.GetMappedPublicPort(5432)};Database=postgres;Username=postgres;Password=test";
        GlobalConfiguration.Configuration.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connection));
        var storage = JobStorage.Current;
        var client = new BackgroundJobClient(storage);
        var original = Convert.ToBase64String("<original>recoverable</original>"u8.ToArray());
        var id = client.Enqueue<RecoveryJob>(job => job.Process(original));
        await _database.StopAsync();
        await _database.StartAsync();
        using var server = new BackgroundJobServer(new BackgroundJobServerOptions
        {
            Queues = ["beskedfordeler"],
            WorkerCount = 1
        }, storage);
        await WaitForState(storage, id, FailedState.StateName);
        using (var db = storage.GetConnection()) Assert.Equal(original, db.GetJobData(id).Job.Args[0]);
        Assert.True(client.ChangeState(id, new EnqueuedState("beskedfordeler"), FailedState.StateName));
        await WaitForState(storage, id, FailedState.StateName);
        using (var db = storage.GetConnection()) Assert.Equal(original, db.GetJobData(id).Job.Args[0]);
    }

    private static async Task WaitForState(JobStorage storage, string id, string state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        while (true)
        {
            try
            {
                using var connection = storage.GetConnection();
                if (connection.GetStateData(id)?.Name == state) return;
            }
            catch (Npgsql.NpgsqlException)
            {
                // Restart invalidates pooled sockets. Require recovery within the same bounded deadline.
            }
            await Task.Delay(100, timeout.Token);
        }
    }

    public sealed class RecoveryJob
    {
        [Queue("beskedfordeler")]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public void Process(string original) => throw new InvalidOperationException("Deliberate failure for recovery validation.");
    }
}
