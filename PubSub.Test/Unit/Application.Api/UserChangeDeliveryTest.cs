using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PubSub.Application.Api.UserSync;
using PubSub.Core.DomainModel.UserSync;
using PubSub.Infrastructure.DataAccess;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PubSub.Test.Unit.Application.Api;

public class UserChangeDeliveryTest
{
    [Fact]
    public async Task Ingress_ConfirmPending_DoesNotAcceptUntilConfirmed()
    {
        await using var db = Database();
        var confirmed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var publisher = new Mock<IUserChangePublisher>();
        publisher.Setup(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 0, false, default)).Returns(confirmed.Task);
        var ingress = new UserChangeIngress(new UserChangeReceiptStore(db), publisher.Object);
        var message = Message();
        var pending = ingress.Enqueue(message, default);
        Assert.False(pending.IsCompleted);
        Assert.Null(Assert.Single(db.UserChangeReceipts).AcceptedAt);
        confirmed.SetResult();
        Assert.NotNull((await pending).AcceptedAt);
        await ingress.Enqueue(message, default);
        publisher.Verify(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 0, false, default), Times.Once);
        await Assert.ThrowsAsync<UserChangeConflictException>(() => ingress.Enqueue(message with { ExternalUserUuid = Guid.NewGuid() }, default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ingress_AmbiguousPublishOrCancellation_LeavesPendingAndRecovers(bool cancelled)
    {
        await using var db = Database();
        var publisher = new Mock<IUserChangePublisher>();
        publisher.Setup(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 0, false, default))
            .ThrowsAsync(cancelled ? new OperationCanceledException() : new UserChangePublishException(new TimeoutException()));
        var ingress = new UserChangeIngress(new UserChangeReceiptStore(db), publisher.Object);
        var message = Message();
        await Assert.ThrowsAnyAsync<Exception>(() => ingress.Enqueue(message, default));
        Assert.Null(Assert.Single(db.UserChangeReceipts).AcceptedAt);
        publisher.Setup(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 0, false, default)).Returns(Task.CompletedTask);
        Assert.NotNull((await ingress.Enqueue(message, default)).AcceptedAt);
    }

    [Fact]
    public void Hash_NormalizedPrecision_IsEquivalentAndVersioned()
    {
        var message = Message() with { OccurredAt = new DateTime(2026, 10, 12, 1, 2, 3, DateTimeKind.Utc).AddTicks(1) };
        Assert.Equal(UserChangeContract.Hash(message), UserChangeContract.Hash(message with { OccurredAt = message.OccurredAt!.Value.AddTicks(8) }));
        Assert.StartsWith("v1:", UserChangeContract.Hash(message));
        Assert.NotEqual(UserChangeContract.Hash(message), UserChangeContract.Hash(message with { OrganizationUuid = Guid.NewGuid() }));
    }

    [Fact]
    public async Task Ingress_AcceptanceCommitFails_RepublishesOnSourceReplay()
    {
        var options = new DbContextOptionsBuilder<PubSubContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var publisher = new Mock<IUserChangePublisher>();
        var message = Message();
        await using (var failing = new AcceptanceFailureDb(options))
            await Assert.ThrowsAsync<DbUpdateException>(() => new UserChangeIngress(new UserChangeReceiptStore(failing), publisher.Object).Enqueue(message, default));
        await using var db = new PubSubContext(options);
        Assert.Null(Assert.Single(db.UserChangeReceipts).AcceptedAt);
        await new UserChangeIngress(new UserChangeReceiptStore(db), publisher.Object).Enqueue(message, default);
        publisher.Verify(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 0, false, default), Times.Exactly(2));
    }

    private sealed class AcceptanceFailureDb(DbContextOptions<PubSubContext> options) : PubSubContext(options)
    {
        private int _saves;
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            ++_saves == 2 ? throw new DbUpdateException("Injected acceptance commit failure") : base.SaveChangesAsync(cancellationToken);
    }

    [Theory]
    [InlineData(200, false, 1, 1)]
    [InlineData(409, true, 1, 1)]
    [InlineData(401, false, 2, 2)]
    [InlineData(403, false, 1, 1)]
    [InlineData(408, false, 1, 1)]
    [InlineData(429, false, 1, 1)]
    [InlineData(503, false, 1, 1)]
    public async Task Consumer_HttpResult_AcksOnlyAfterSuccessOrConfirmedTransfer(int status, bool dead, int sends, int logins)
    {
        var handler = new RecordingHandler { Status = (HttpStatusCode)status };
        var publisher = new Mock<IUserChangePublisher>();
        var channel = new Mock<IChannel>();
        var transferred = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        publisher.Setup(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 1, dead, default)).Returns(transferred.Task);
        using var consumer = Consumer(handler, publisher.Object);
        var pending = consumer.Handle(channel.Object, Delivery(), new Uri("https://kitos.test/callback"), default);
        if (status != 200)
        {
            Assert.False(pending.IsCompleted);
            channel.Verify(x => x.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
            transferred.SetResult();
        }
        await pending;
        channel.Verify(x => x.BasicAckAsync(1, false, default), Times.Once);
        publisher.Verify(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 1, dead, default), status == 200 ? Times.Never() : Times.Once());
        Assert.Equal(sends, handler.Sends);
        Assert.Equal(logins, handler.Logins);
    }

    [Theory]
    [InlineData(409)]
    [InlineData(503)]
    public async Task Consumer_TransferFails_LeavesOriginalUnacked(int status)
    {
        var publisher = new Mock<IUserChangePublisher>();
        publisher.Setup(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), default))
            .ThrowsAsync(new UserChangePublishException(new TimeoutException()));
        var channel = new Mock<IChannel>();
        using var consumer = Consumer(new RecordingHandler { Status = (HttpStatusCode)status }, publisher.Object);
        await Assert.ThrowsAsync<UserChangePublishException>(() => consumer.Handle(channel.Object, Delivery(), new Uri("https://kitos.test/callback"), default));
        channel.Verify(x => x.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consumer_InvalidContract_DeadLettersWithoutHttp()
    {
        var handler = new RecordingHandler();
        var publisher = new Mock<IUserChangePublisher>();
        using var consumer = Consumer(handler, publisher.Object);
        var message = new BasicDeliverEventArgs("tag", 1, false, "", "queue", new BasicProperties(), new byte[] { 0 }, default);
        await consumer.Handle(Mock.Of<IChannel>(), message, new Uri("https://kitos.test/callback"), default);
        Assert.Equal(0, handler.Sends);
        publisher.Verify(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "", 1, true, default), Times.Once);
    }

    private static PubSubContext Database() => new(new DbContextOptionsBuilder<PubSubContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UserDeletionEvent Message() => new("id", Guid.NewGuid(), Guid.NewGuid());
    private static BasicDeliverEventArgs Delivery() => new("tag", 1, false, "", "queue",
        new BasicProperties { MessageId = "id", Type = UserChangeContract.Version }, UserChangeContract.Serialize(Message()), default);
    private static UserChangeConsumer Consumer(RecordingHandler handler, IUserChangePublisher publisher)
    {
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(x => x.CreateClient("UserSync")).Returns(() => new HttpClient(handler, false));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["UserSync:KitosEmail"] = "pubsub@test", ["UserSync:KitosPassword"] = "test-password" }).Build();
        return new UserChangeConsumer(Mock.Of<IConnectionFactory>(), new UserChangeTopology(config), publisher,
            new KitosUserChangeDelivery(clients.Object, config), config, NullLogger<UserChangeConsumer>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public int Sends { get; private set; }
        public int Logins { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/api/authorize/GetToken")
            {
                Logins++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(new { Response = new { Token = "test-token", Expires = DateTime.UtcNow.AddHours(24), LoginSuccessful = true } }) });
            }
            Sends++;
            Assert.Equal("Bearer test-token", request.Headers.Authorization!.ToString());
            return Task.FromResult(new HttpResponseMessage(Status));
        }
    }
}
