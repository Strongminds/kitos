using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using PubSub.Application.Api.UserSync;

namespace PubSub.Test.Unit.Application.Api;

public class KitosTokenProviderTest
{
    [Fact]
    public async Task CachesTokenAndRenewsBeforeExpiryOrAfterInvalidation()
    {
        var clock = new Clock();
        var handler = new LoginHandler(clock);
        var provider = Create(handler, clock);
        var endpoint = new Uri("https://kitos.test/api/v2/integrations/fk-organisation/user-changes");
        Assert.Equal("token-1", await provider.GetToken(endpoint, default));
        Assert.Equal("token-1", await provider.GetToken(endpoint, default));
        Assert.Equal(1, handler.Calls);
        clock.Now = clock.Now.AddHours(24).AddSeconds(-30);
        Assert.Equal("token-2", await provider.GetToken(endpoint, default));
        provider.Invalidate();
        Assert.Equal("token-3", await provider.GetToken(endpoint, default));
    }

    [Theory]
    [InlineData(401, false)]
    [InlineData(200, true)]
    public async Task FailedLoginOrMalformedTokenIsNeverCached(int status, bool malformed)
    {
        var clock = new Clock();
        var handler = new LoginHandler(clock) { Status = (HttpStatusCode)status, Malformed = malformed };
        var provider = Create(handler, clock);
        var endpoint = new Uri("https://kitos.test/callback");
        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetToken(endpoint, default));
        handler.Status = HttpStatusCode.OK;
        handler.Malformed = false;
        Assert.Equal("token-2", await provider.GetToken(endpoint, default));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"response\":null}")]
    [InlineData("{\"response\":{\"token\":\"\",\"loginSuccessful\":true,\"expires\":\"2099-01-01T00:00:00Z\"}}")]
    [InlineData("{\"response\":{\"token\":\"test\",\"loginSuccessful\":false,\"expires\":\"2099-01-01T00:00:00Z\"}}")]
    [InlineData("{\"response\":{\"token\":\"test\",\"loginSuccessful\":true,\"expires\":\"2000-01-01T00:00:00Z\"}}")]
    [InlineData("{\"token\":\"test\",\"loginSuccessful\":true,\"expires\":\"2099-01-01T00:00:00Z\"}")]
    public async Task InvalidTokenEnvelopeIsNeverCached(string body)
    {
        var clock = new Clock();
        var handler = new LoginHandler(clock) { Body = body };
        var provider = Create(handler, clock);
        var endpoint = new Uri("https://kitos.test/callback");
        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetToken(endpoint, default));
        handler.Body = null;
        Assert.Equal("token-2", await provider.GetToken(endpoint, default));
    }

    private static KitosTokenProvider Create(LoginHandler handler, Clock clock)
    {
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(x => x.CreateClient("UserSync")).Returns(() => new HttpClient(handler, false));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["UserSync:KitosEmail"] = "pubsub@test", ["UserSync:KitosPassword"] = "test-password" }).Build();
        return new KitosTokenProvider(clients.Object, config, clock);
    }

    private class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private class LoginHandler(Clock clock) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public bool Malformed { get; set; }
        public string? Body { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal("https://kitos.test/api/authorize/GetToken", request.RequestUri!.AbsoluteUri);
            Assert.Equal(HttpMethod.Post, request.Method);
            var credentials = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
            Assert.Equal("pubsub@test", credentials.GetProperty("email").GetString());
            Assert.Equal("test-password", credentials.GetProperty("password").GetString());
            return new HttpResponseMessage(Status)
            {
                Content = Body != null ? new StringContent(Body) : Malformed ? new StringContent("not-json") : JsonContent.Create(new
                { Response = new { Token = $"token-{Calls}", LoginSuccessful = true, Expires = clock.Now.AddHours(24) } })
            };
        }
    }
}
