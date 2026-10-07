using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PubSub.Application.Api.UserSync;
using PubSub.Core.DomainModel.UserSync;
using PubSub.Infrastructure.DataAccess;

namespace PubSub.Test.Unit.Application.Api;

public class UserChangeDeliveryTest
{
    [Fact]
    public async Task DurableDeliveryRetriesFailureAndPreservesPayloadAndToken()
    {
        var database = Guid.NewGuid().ToString();
        using var services = new ServiceCollection().AddDbContext<PubSubContext>(x => x.UseInMemoryDatabase(database)).BuildServiceProvider();
        var message = new UserDeletionEvent("id-1", Guid.NewGuid(), Guid.NewGuid());
        using (var scope = services.CreateScope())
            await new UserChangeOutbox(scope.ServiceProvider.GetRequiredService<PubSubContext>()).Enqueue(message, default);
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(x => x.CreateClient("UserSync")).Returns(() => new HttpClient(handler, false));
        var worker = new UserChangeDeliveryWorker(services.GetRequiredService<IServiceScopeFactory>(), clients.Object,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["UserSync:KitosEmail"] = "pubsub@test", ["UserSync:KitosPassword"] = "test-password" }).Build(), NullLogger<UserChangeDeliveryWorker>.Instance);

        await worker.DeliverBatch(new Uri("https://kitos.test/callback"), default);
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PubSubContext>();
            var row = await db.UserChangeDeliveries.SingleAsync();
            Assert.Null(row.DeliveredAt);
            Assert.False(row.DeadLettered);
            row.NextAttemptAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        handler.Status = HttpStatusCode.OK;
        await worker.DeliverBatch(new Uri("https://kitos.test/callback"), default);
        using (var scope = services.CreateScope())
        {
            var row = await scope.ServiceProvider.GetRequiredService<PubSubContext>().UserChangeDeliveries.SingleAsync();
            Assert.NotNull(row.DeliveredAt);
            Assert.Equal(2, row.Attempts);
        }
        Assert.Equal(2, handler.Payloads.Count);
        Assert.Equal(handler.Payloads[0], handler.Payloads[1]);
        Assert.All(handler.Tokens, token => Assert.Equal("Bearer test-token", token));
        Assert.Equal(1, handler.Logins);
    }

    [Theory]
    [InlineData(401, 2, 2)]
    [InlineData(403, 1, 1)]
    public async Task AuthenticationFailuresRemainRetryableAndOnly401RenewsToken(int status, int sends, int logins)
    {
        var databaseName = Guid.NewGuid().ToString();
        using var stableServices = new ServiceCollection().AddDbContext<PubSubContext>(x => x.UseInMemoryDatabase(databaseName)).BuildServiceProvider();
        using var stableScope = stableServices.CreateScope();
        var db = stableScope.ServiceProvider.GetRequiredService<PubSubContext>();
        await new UserChangeOutbox(db).Enqueue(new UserDeletionEvent("auth-failure", Guid.NewGuid(), Guid.NewGuid()), default);
        var handler = new RecordingHandler { Status = (HttpStatusCode)status };
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(x => x.CreateClient("UserSync")).Returns(() => new HttpClient(handler, false));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["UserSync:KitosEmail"] = "pubsub@test", ["UserSync:KitosPassword"] = "test-password" }).Build();
        var worker = new UserChangeDeliveryWorker(stableServices.GetRequiredService<IServiceScopeFactory>(), clients.Object,
            config, NullLogger<UserChangeDeliveryWorker>.Instance);
        await worker.DeliverBatch(new Uri("https://kitos.test/callback"), default);
        db.ChangeTracker.Clear();
        var delivery = await db.UserChangeDeliveries.SingleAsync();
        Assert.Null(delivery.DeliveredAt);
        Assert.False(delivery.DeadLettered);
        Assert.Equal(status, delivery.LastStatusCode);
        Assert.Equal(sends, handler.Payloads.Count);
        Assert.Equal(logins, handler.Logins);
    }

    [Fact]
    public async Task DuplicateFixtureCreatesOneDeliveryAndConflictingReuseIsRejected()
    {
        using var db = new PubSubContext(new DbContextOptionsBuilder<PubSubContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var outbox = new UserChangeOutbox(db);
        var message = new UserDeletionEvent("id", Guid.NewGuid(), Guid.NewGuid());
        var first = await outbox.Enqueue(message, default);
        Assert.Equal(first.Uuid, (await outbox.Enqueue(message, default)).Uuid);
        Assert.Single(db.UserChangeDeliveries);
        await Assert.ThrowsAsync<InvalidOperationException>(() => outbox.Enqueue(message with { ExternalUserUuid = Guid.NewGuid() }, default));
    }

    [Theory]
    [InlineData(400, true)]
    [InlineData(409, true)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    [InlineData(408, false)]
    [InlineData(429, false)]
    [InlineData(503, false)]
    public void ClassifiesDeliveryFailures(int status, bool permanent) =>
        Assert.Equal(permanent, UserChangeDeliveryWorker.IsPermanentFailure((HttpStatusCode)status));

    private class RecordingHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.ServiceUnavailable;
        public List<string> Payloads { get; } = [];
        public List<string> Tokens { get; } = [];
        public int Logins { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/api/authorize/GetToken")
            {
                Logins++;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(new { Response = new { Token = "test-token", Expires = DateTime.UtcNow.AddHours(24), LoginSuccessful = true } }) };
            }
            Payloads.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            Tokens.Add(request.Headers.Authorization!.ToString());
            return new HttpResponseMessage(Status);
        }
    }
}
