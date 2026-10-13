using UserSync.Transport;
using Microsoft.EntityFrameworkCore;
using Moq;
using PubSub.Application.Api.UserSync;
using PubSub.Infrastructure.DataAccess;

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

    private static PubSubContext Database() => new(new DbContextOptionsBuilder<PubSubContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UserDeletionEvent Message() => new("id", Guid.NewGuid(), Guid.NewGuid());
}
