using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UserSync.Transport;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure.UserSync;

public class UserChangeConsumerTest
{
    [Theory]
    [InlineData(UserChangeDeliveryResult.Accepted)]
    [InlineData(UserChangeDeliveryResult.Rejected)]
    [InlineData(UserChangeDeliveryResult.Retry)]
    public async Task Handle_AcknowledgesOnlyAfterDurableDeliveryOrConfirmedTransfer(UserChangeDeliveryResult result)
    {
        var delivered = new TaskCompletionSource<UserChangeDeliveryResult>();
        var confirmed = new TaskCompletionSource();
        var delivery = new Mock<IUserChangeDelivery>();
        delivery.Setup(x => x.Deliver(It.IsAny<UserDeletionEvent>(), default)).Returns(delivered.Task);
        var publisher = new Mock<IUserChangePublisher>();
        publisher.Setup(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 1,
            result == UserChangeDeliveryResult.Rejected, default)).Returns(confirmed.Task);
        var channel = new Mock<IChannel>();
        using var consumer = Consumer(delivery.Object, publisher.Object);
        var pending = consumer.Handle(channel.Object, Message(), default);
        channel.Verify(x => x.BasicAckAsync(1, false, default), Times.Never);
        delivered.SetResult(result);
        if (result != UserChangeDeliveryResult.Accepted)
        {
            Assert.False(pending.IsCompleted);
            channel.Verify(x => x.BasicAckAsync(1, false, default), Times.Never);
            confirmed.SetResult();
        }
        await pending;
        channel.Verify(x => x.BasicAckAsync(1, false, default), Times.Once);
    }

    [Fact]
    public async Task Handle_FailedTransfer_LeavesOriginalUnacknowledged()
    {
        var delivery = new Mock<IUserChangeDelivery>();
        delivery.Setup(x => x.Deliver(It.IsAny<UserDeletionEvent>(), default)).ThrowsAsync(new InvalidOperationException());
        var publisher = new Mock<IUserChangePublisher>();
        publisher.Setup(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 1, false, default))
            .ThrowsAsync(new UserChangePublishException(new TimeoutException()));
        var channel = new Mock<IChannel>();
        using var consumer = Consumer(delivery.Object, publisher.Object);
        await Assert.ThrowsAsync<UserChangePublishException>(() => consumer.Handle(channel.Object, Message(), default));
        channel.Verify(x => x.BasicAckAsync(1, false, default), Times.Never);
    }

    [Theory]
    [InlineData("invalid", "id", false)]
    [InlineData(UserChangeContract.Version, "different", false)]
    [InlineData(UserChangeContract.Version, "id", true)]
    public async Task Handle_InvalidContract_DeadLettersWithoutIngesting(string version, string id, bool malformed)
    {
        var delivery = new Mock<IUserChangeDelivery>(MockBehavior.Strict);
        var publisher = new Mock<IUserChangePublisher>();
        using var consumer = Consumer(delivery.Object, publisher.Object);
        var message = Message(version, id, malformed);
        await consumer.Handle(Mock.Of<IChannel>(), message, default);
        publisher.Verify(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), id, 1, true, default), Times.Once);
    }

    [Fact]
    public async Task Handle_ApplicationArgumentException_RetriesInsteadOfRejecting()
    {
        var delivery = new Mock<IUserChangeDelivery>();
        delivery.Setup(x => x.Deliver(It.IsAny<UserDeletionEvent>(), default)).ThrowsAsync(new ArgumentException());
        var publisher = new Mock<IUserChangePublisher>();
        using var consumer = Consumer(delivery.Object, publisher.Object);
        await consumer.Handle(Mock.Of<IChannel>(), Message(), default);
        publisher.Verify(x => x.Publish(It.IsAny<ReadOnlyMemory<byte>>(), "id", 1, false, default), Times.Once);
    }

    [Fact]
    public async Task Handle_ShutdownDuringIngestion_DoesNotAcknowledgeOrTransfer()
    {
        using var cancellation = new CancellationTokenSource();
        var delivery = new Mock<IUserChangeDelivery>();
        delivery.Setup(x => x.Deliver(It.IsAny<UserDeletionEvent>(), cancellation.Token))
            .Returns(async () =>
            {
                await cancellation.CancelAsync();
                cancellation.Token.ThrowIfCancellationRequested();
                return UserChangeDeliveryResult.Accepted;
            });
        var publisher = new Mock<IUserChangePublisher>(MockBehavior.Strict);
        var channel = new Mock<IChannel>(MockBehavior.Strict);
        using var consumer = Consumer(delivery.Object, publisher.Object);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer.Handle(channel.Object, Message(), cancellation.Token));
        Assert.Empty(channel.Invocations);
        Assert.Empty(publisher.Invocations);
    }
    private static BasicDeliverEventArgs Message(string version = UserChangeContract.Version, string id = "id", bool malformed = false) =>
        new("tag", 1, false, "", "queue", new BasicProperties { Type = version, MessageId = id },
            malformed ? new byte[] { 0 } : UserChangeContract.Serialize(new UserDeletionEvent("id", Guid.NewGuid(), Guid.NewGuid())), default);

    private static UserChangeConsumer Consumer(IUserChangeDelivery delivery, IUserChangePublisher publisher)
    {
        var config = new ConfigurationBuilder().Build();
        return new UserChangeConsumer(Mock.Of<IConnectionFactory>(), new UserChangeTopology(config), publisher,
            delivery, config, NullLogger<UserChangeConsumer>.Instance);
    }
}
