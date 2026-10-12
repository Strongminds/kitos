using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PubSub.Application.Api.UserSync;
using PubSub.Core.DomainModel.UserSync;
using PubSub.Infrastructure.DataAccess;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Net;

namespace Tests.Container.Tests;

[Collection(MigrationTestsCollection.Name)]
public sealed class UserSyncRabbitMqTest : IAsyncLifetime
{
    private readonly IContainer _broker = new ContainerBuilder("rabbitmq:4.1-management")
        .WithEnvironment("RABBITMQ_DEFAULT_USER", "test")
        .WithEnvironment("RABBITMQ_DEFAULT_PASS", "test")
        .WithPortBinding(5672, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5672)).Build();
    private readonly IContainer _database = new ContainerBuilder("postgres:16-alpine")
        .WithEnvironment("POSTGRES_PASSWORD", "test")
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432)).Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_broker.StartAsync(), _database.StartAsync());
        await using var db = Database();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _broker.DisposeAsync();
        await _database.DisposeAsync();
    }

    private ConnectionFactory Factory() => new()
    {
        HostName = _broker.Hostname,
        Port = _broker.GetMappedPublicPort(5672),
        UserName = "test",
        Password = "test",
        AutomaticRecoveryEnabled = false
    };

    private PubSubContext Database() => new(new DbContextOptionsBuilder<PubSubContext>().UseNpgsql(
        $"Host={_database.Hostname};Port={_database.GetMappedPublicPort(5432)};Database=postgres;Username=postgres;Password=test").Options);

    private static UserChangeTopology Topology() => new(new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> { ["UserSync:QueuePrefix"] = "test." + Guid.NewGuid().ToString("N") }).Build());

    [Fact]
    public async Task Consumer_KitosUnavailableThenBrokerRestart_ReconnectsAndDeliversIdempotently()
    {
        var topology = Topology();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["UserSync:Enabled"] = "true",
            ["UserSync:KitosEndpoint"] = "https://kitos.test/callback",
            ["UserSync:KitosEmail"] = "test",
            ["UserSync:KitosPassword"] = "test"
        }).Build();
        var clients = new KitosClients();
        await using var publisher = new UserChangePublisher(Factory(), topology);
        using var consumer = new UserChangeConsumer(Factory(), topology, publisher,
            new KitosUserChangeDelivery(clients, config), config, NullLogger<UserChangeConsumer>.Instance);
        var message = new UserDeletionEvent("recover", Guid.NewGuid(), Guid.NewGuid());
        await using (var db = Database())
            Assert.NotNull((await new UserChangeIngress(new UserChangeReceiptStore(db), publisher).Enqueue(message, default)).AcceptedAt);
        await consumer.StartAsync(default);
        try
        {
            await clients.Handler.Attempted.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await _broker.StopAsync();
            await _broker.StartAsync();
            clients.Handler.Available = true;
            await clients.Handler.Accepted.Task.WaitAsync(TimeSpan.FromSeconds(60));
            // Simulate source/transfer ambiguity by delivering the same identity again.
            await publisher.Publish(UserChangeContract.Serialize(message), message.ExternalMessageId, 0, false, default);
            await clients.Handler.Duplicate.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Single(clients.Handler.Changes);
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await consumer.StopAsync(timeout.Token);
        }
    }

    private sealed class KitosClients : IHttpClientFactory
    {
        public KitosHandler Handler { get; } = new();
        public HttpClient CreateClient(string name) => new(Handler, false);
    }

    private sealed class KitosHandler : HttpMessageHandler
    {
        public volatile bool Available;
        public HttpStatusCode UnavailableStatus { get; set; } = HttpStatusCode.ServiceUnavailable;
        public ConcurrentDictionary<string, bool> Changes { get; } = new();
        public TaskCompletionSource Attempted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Accepted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Duplicate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/api/authorize/GetToken")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = System.Net.Http.Json.JsonContent.Create(new { Response = new { Token = "test", Expires = DateTime.UtcNow.AddHours(1), LoginSuccessful = true } })
                };
            Attempted.TrySetResult();
            if (!Available) return new HttpResponseMessage(UnavailableStatus);
            var message = UserChangeContract.Read(await request.Content!.ReadAsByteArrayAsync(cancellationToken));
            if (!Changes.TryAdd(message.ExternalMessageId, true)) Duplicate.TrySetResult();
            Accepted.TrySetResult();
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Receipt_ConcurrentReservations_RejectConflictsAndRecoverAcrossContexts()
    {
        var message = new UserDeletionEvent(Guid.NewGuid().ToString(), Guid.NewGuid(), Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow);
        var topology = Topology();
        await using var publisher = new UserChangePublisher(Factory(), topology);
        async Task Accept(UserDeletionEvent input)
        {
            await using var db = Database();
            await new UserChangeIngress(new UserChangeReceiptStore(db), publisher).Enqueue(input, default);
        }
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Accept(message)));
        await Assert.ThrowsAsync<UserChangeConflictException>(() => Accept(message with { ExternalUserUuid = Guid.NewGuid() }));
        await using var check = Database();
        Assert.NotNull((await check.UserChangeReceipts.SingleAsync(x => x.ExternalMessageId == message.ExternalMessageId)).AcceptedAt);
        // Simulate a crash after reservation, then after broker confirmation but before receipt acceptance.
        var pending = message with { ExternalMessageId = Guid.NewGuid().ToString() };
        await new UserChangeReceiptStore(check).Reserve(pending, default);
        await publisher.Publish(UserChangeContract.Serialize(pending), pending.ExternalMessageId, 0, false, default);
        await Accept(pending);
        await Accept(pending); // Accepted but response lost: no further publish required.
        Assert.Equal(2, await check.UserChangeReceipts.CountAsync());
    }

    [Fact]
    public async Task Receipt_ConcurrentDifferentEvents_ExactlyOneReservesIdentity()
    {
        var message = new UserDeletionEvent("conflict", Guid.NewGuid(), Guid.NewGuid());
        async Task<bool> Reserve(UserDeletionEvent input)
        {
            await using var db = Database();
            try { await new UserChangeReceiptStore(db).Reserve(input, default); return true; }
            catch (UserChangeConflictException) { return false; }
        }
        var results = await Task.WhenAll(Reserve(message), Reserve(message with { ExternalUserUuid = Guid.NewGuid() }));
        Assert.Single(results, x => x);
        await using var check = Database();
        Assert.Null((await check.UserChangeReceipts.SingleAsync()).AcceptedAt);
    }

    [Fact]
    public async Task Publisher_MandatoryReturnAndNack_DoNotConfirm()
    {
        await using var connection = await Factory().CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true));
        await Assert.ThrowsAsync<PublishException>(async () => await channel.BasicPublishAsync("", "missing", true,
            new BasicProperties { Persistent = true }, Encoding.UTF8.GetBytes("test")));
        await channel.QueueDeclareAsync("full", true, false, false, new Dictionary<string, object?>
        { ["x-queue-type"] = "quorum", ["x-max-length"] = 1, ["x-overflow"] = "reject-publish" });
        await channel.BasicPublishAsync("", "full", true, new BasicProperties { Persistent = true }, new byte[] { 1 });
        // Quorum length limits can overshoot while the queue propagates its reject-publish state.
        var rejected = false;
        for (var attempt = 0; attempt < 100 && !rejected; attempt++)
        {
            try { await channel.BasicPublishAsync("", "full", true, new BasicProperties { Persistent = true }, new byte[] { 2 }); }
            catch (PublishException) { rejected = true; }
        }
        Assert.True(rejected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Consumer_TargetRejectsTransfer_OriginalIsRedelivered(bool dead)
    {
        var topology = Topology();
        await using var publisher = new UserChangePublisher(Factory(), topology);
        var message = new UserDeletionEvent("transfer", Guid.NewGuid(), Guid.NewGuid());
        var body = UserChangeContract.Serialize(message);
        await publisher.Publish(body, message.ExternalMessageId, 0, false, default);
        await publisher.Publish(body, message.ExternalMessageId, 8, dead, default);
        var target = dead ? topology.DeadLetter : topology.Retry(8);
        var policy = await _broker.ExecAsync(new[] { "rabbitmqctl", "set_policy", "full-target",
            "^" + System.Text.RegularExpressions.Regex.Escape(target) + "$", "{\"max-length\":0,\"overflow\":\"reject-publish\"}", "--apply-to", "quorum_queues" });
        Assert.Equal(0, policy.ExitCode);
        await Task.Delay(500);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["UserSync:KitosEmail"] = "test", ["UserSync:KitosPassword"] = "test" }).Build();
        var clients = new KitosClients();
        clients.Handler.UnavailableStatus = dead ? HttpStatusCode.Conflict : HttpStatusCode.ServiceUnavailable;
        using var consumer = new UserChangeConsumer(Factory(), topology, publisher,
            new KitosUserChangeDelivery(clients, config), config, NullLogger<UserChangeConsumer>.Instance);
        await using (var connection = await Factory().CreateConnectionAsync())
        await using (var channel = await connection.CreateChannelAsync())
        {
            var original = await Get(channel, topology.Main);
            var properties = new BasicProperties(original.BasicProperties)
            { Headers = new Dictionary<string, object?> { ["user-sync-attempt"] = 8 } };
            var delivery = new BasicDeliverEventArgs("tag", original.DeliveryTag, false, "", topology.Main, properties, original.Body, default);
            await Assert.ThrowsAsync<UserChangePublishException>(() => consumer.Handle(channel, delivery, new Uri("https://kitos.test/callback"), default));
        }
        await using var recovered = await Factory().CreateConnectionAsync();
        await using var recoveredChannel = await recovered.CreateChannelAsync();
        Assert.True((await Get(recoveredChannel, topology.Main)).Redelivered);
    }

    [Fact]
    public async Task Broker_RestartAndUnackedConsumerTermination_PreserveConfirmedMessage()
    {
        var topology = Topology();
        var message = new UserDeletionEvent("restart", Guid.NewGuid(), Guid.NewGuid());
        await using (var publisher = new UserChangePublisher(Factory(), topology))
            await publisher.Publish(UserChangeContract.Serialize(message), message.ExternalMessageId, 0, false, default);
        await _broker.StopAsync();
        await _broker.StartAsync();
        await using (var connection = await Factory().CreateConnectionAsync())
        await using (var channel = await connection.CreateChannelAsync())
        {
            var first = await Get(channel, topology.Main);
            Assert.Equal(message.ExternalMessageId, first.BasicProperties.MessageId);
            Assert.True(first.BasicProperties.Persistent);
            // Close the consumer connection without ack, like a process dying after KITOS committed.
        }
        await using var recovered = await Factory().CreateConnectionAsync();
        await using var recoveredChannel = await recovered.CreateChannelAsync();
        var second = await Get(recoveredChannel, topology.Main);
        Assert.True(second.Redelivered);
        Assert.Equal(message, UserChangeContract.Read(second.Body.Span));
        await recoveredChannel.BasicAckAsync(second.DeliveryTag, false);
    }

    [Fact]
    public async Task Retry_TargetUnavailable_RetainsMessageUntilMainReturns()
    {
        var topology = Topology();
        await using var publisher = new UserChangePublisher(Factory(), topology);
        var message = new UserDeletionEvent("retry", Guid.NewGuid(), Guid.NewGuid());
        await publisher.Publish(UserChangeContract.Serialize(message), message.ExternalMessageId, 1, false, default);
        await using var connection = await Factory().CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(topology.Main, false, false);
        await Task.Delay(TimeSpan.FromSeconds(4));
        // At-least-once dead lettering keeps expired messages while the routing target is absent.
        await topology.Declare(channel, default);
        var retried = await Get(channel, topology.Main, 240);
        Assert.Equal(message.ExternalMessageId, retried.BasicProperties.MessageId);
        await channel.BasicAckAsync(retried.DeliveryTag, false);
    }

    [Fact]
    public async Task Cutover_EmptyPopulatedAndRestarted_PreservesHistoryAndReconcilesAllRows()
    {
        var topology = Topology();
        await using var publisher = new UserChangePublisher(Factory(), topology);
        await using var db = Database();
        var cutover = new UserChangeCutover(db, new UserChangeReceiptStore(db), publisher);
        Assert.Equal(0, await cutover.Run(default));
        foreach (var state in new[] { "delivered", "pending", "dead" })
        {
            var message = new UserDeletionEvent(state, Guid.NewGuid(), Guid.NewGuid());
            db.UserChangeDeliveries.Add(new UserChangeDelivery
            {
                ExternalMessageId = state,
                Payload = Encoding.UTF8.GetString(UserChangeContract.Serialize(message)),
                DeliveredAt = state == "delivered" ? DateTime.UtcNow : null,
                DeadLettered = state == "dead"
            });
        }
        await db.SaveChangesAsync();
        var interrupted = new UserChangeCutover(db, new UserChangeReceiptStore(db), new FailOncePublisher(publisher));
        await Assert.ThrowsAsync<UserChangePublishException>(() => interrupted.Run(default));
        db.ChangeTracker.Clear();
        await cutover.Run(default);
        Assert.Equal(0, await cutover.Run(default));
        Assert.Equal(3, await db.UserChangeDeliveries.CountAsync(x => x.RabbitMqMigratedAt != null));
        Assert.Equal(3, await db.UserChangeReceipts.CountAsync(x => x.AcceptedAt != null));
        await using var connection = await Factory().CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        Assert.Equal("pending", (await Get(channel, topology.Main)).BasicProperties.MessageId);
        Assert.Equal("dead", (await Get(channel, topology.DeadLetter)).BasicProperties.MessageId);
    }

    private sealed class FailOncePublisher(IUserChangePublisher inner) : IUserChangePublisher
    {
        public async Task Publish(ReadOnlyMemory<byte> body, string messageId, int attempt, bool deadLetter, CancellationToken cancellationToken)
        {
            await inner.Publish(body, messageId, attempt, deadLetter, cancellationToken);
            throw new UserChangePublishException(new TimeoutException());
        }
    }

    private static async Task<BasicGetResult> Get(IChannel channel, string queue, int seconds = 30)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        while (true)
        {
            var result = await channel.BasicGetAsync(queue, false, timeout.Token);
            if (result != null) return result;
            await Task.Delay(100, timeout.Token);
        }
    }
}
