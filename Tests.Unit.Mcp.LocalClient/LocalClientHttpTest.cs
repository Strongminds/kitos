using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tools.Mcp.LocalClient;
using Xunit;

namespace Tests.Unit.Mcp.LocalClient
{
    public class LocalClientHttpTest
    {
        [Fact]
        public async Task Connect_ActualHttpsForm_RedirectsToSdkAuthorizationAndAllowsPendingRetry()
        {
            await using var fixture = await InspectorFixture.Create();
            var csrf = await fixture.LoadPage();
            using var connected = await fixture.Post("/connect", csrf, fixture.BrowserPostOrigin);
            Assert.Equal(HttpStatusCode.Redirect, connected.StatusCode);
            var authorization = connected.Headers.Location!;
            Assert.Equal(fixture.BrokerOrigin.AuthorityUri(), authorization.GetLeftPart(UriPartial.Authority));
            Assert.Equal("/authorize", authorization.AbsolutePath);
            Assert.Contains("code_challenge_method=S256", authorization.Query);
            Assert.Contains("client_id=kitos-local-pilot", authorization.Query);
            Assert.Contains("state=", authorization.Query);
            using var retried = await fixture.Post("/connect", csrf, fixture.BrowserPostOrigin);
            Assert.Equal(HttpStatusCode.Redirect, retried.StatusCode);
            Assert.Equal(authorization, retried.Headers.Location);
        }

        [Fact]
        public async Task Connect_FailedDependency_CanStartFreshAuthorization()
        {
            await using var fixture = await InspectorFixture.Create();
            fixture.FailBroker = true;
            var csrf = await fixture.LoadPage();
            using var failed = await fixture.Post("/connect", csrf, fixture.BrowserPostOrigin);
            Assert.Equal(HttpStatusCode.Redirect, failed.StatusCode);
            Assert.Equal("/", failed.Headers.Location!.OriginalString);
            using var failurePage = await fixture.GetPage();
            var failureHtml = await failurePage.Content.ReadAsStringAsync();
            Assert.Contains("Connection failed during MCP connection (HTTP 500)", failureHtml);
            Assert.DoesNotContain("fixture-credential-must-not-leak", failureHtml);
            fixture.FailBroker = false;
            using var retry = await fixture.Post("/connect", csrf, fixture.BrowserPostOrigin);
            Assert.Equal(HttpStatusCode.Redirect, retry.StatusCode);
            Assert.Equal("/authorize", retry.Headers.Location!.AbsolutePath);
        }

        [Theory]
        [InlineData("null", "valid")]
        [InlineData("https://untrusted.example", "valid")]
        [InlineData("same", "missing")]
        [InlineData("same", "stale")]
        public async Task Connect_InvalidOriginOrCsrf_ReturnsVisibleDeniedPage(string origin, string token)
        {
            await using var fixture = await InspectorFixture.Create();
            var csrf = await fixture.LoadPage();
            using var rejected = await fixture.Post("/connect", token == "valid" ? csrf : token == "missing" ? null : "stale", origin == "same" ? fixture.Origin.AuthorityUri() : origin);
            Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
            Assert.StartsWith("text/html", rejected.Content.Headers.ContentType!.ToString());
            Assert.Contains("reload", await rejected.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("untrusted.example", await rejected.Content.ReadAsStringAsync());
            Assert.Equal(0, fixture.BrokerRequests);
        }

        [Fact]
        public async Task Connect_CachedClientAfterBrokerStateLoss_StartsFreshAuthorizationAndClearsResults()
        {
            await using var fixture = await InspectorFixture.Create();
            fixture.AcceptMcp = true;
            var csrf = await fixture.LoadPage();
            using var connected = await fixture.Post("/connect", csrf, fixture.BrowserPostOrigin);
            Assert.Equal("Connected. Query a system name below.", fixture.Status);
            fixture.AcceptMcp = false;
            using var reconnected = await fixture.Post("/connect", csrf, fixture.BrowserPostOrigin);
            Assert.True(reconnected.Headers.Location!.IsAbsoluteUri);
            Assert.Equal(fixture.BrokerOrigin.AuthorityUri(), reconnected.Headers.Location!.GetLeftPart(UriPartial.Authority));
            Assert.Equal("/authorize", reconnected.Headers.Location.AbsolutePath);
            Assert.Null(fixture.Result);
        }

        [Fact]
        public async Task Connect_HumanTokenEntryPause_PreservesStateUntilTokenExchange()
        {
            await using var fixture = await InspectorFixture.Create();
            var csrf = await fixture.LoadPage();
            using var connected = await fixture.Post("/connect", csrf, fixture.BrowserPostOrigin);
            var authorization = connected.Headers.Location!;
            var state = QueryHelpers.ParseQuery(authorization.Query)["state"].ToString();
            // Exceed the SDK's default five-second discovery probe to model manual token entry.
            await Task.Delay(TimeSpan.FromSeconds(8));
            using var callback = await fixture.Callback(state);
            Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (fixture.Status == "Connecting") await Task.Delay(20, deadline.Token);
            Assert.Equal(1, fixture.TokenExchangeRequests);
            Assert.Equal("Connection failed during OAuth token exchange (HTTP 500). Try Connect again.", fixture.Status);
        }

        private sealed class InspectorFixture : IAsyncDisposable
        {
            private readonly X509Certificate2 _certificate;
            private readonly WebApplication _broker;
            private readonly WebApplication _inspector;
            private readonly PilotSession _pilot;
            private readonly HttpClient _http;
            internal Uri Origin { get; }
            internal Uri BrokerOrigin { get; }
            internal string BrowserPostOrigin { get; private set; } = "null";
            internal bool FailBroker { get; set; }
            internal bool AcceptMcp { get; set; }
            internal int BrokerRequests { get; private set; }
            internal int TokenExchangeRequests { get; private set; }
            internal string Status => _pilot.Status;
            internal string? Result => _pilot.Result;

            private InspectorFixture(X509Certificate2 certificate, WebApplication broker, WebApplication inspector, PilotSession pilot, HttpClient http, Uri origin, Uri brokerOrigin)
            {
                _certificate = certificate; _broker = broker; _inspector = inspector; _pilot = pilot; _http = http; Origin = origin; BrokerOrigin = brokerOrigin;
            }

            internal static async Task<InspectorFixture> Create()
            {
                using var rsa = RSA.Create(2048);
                var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost"); request.CertificateExtensions.Add(names.Build());
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
                using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
                var certificate = X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
                var brokerOrigin = new Uri($"https://localhost:{Port()}");
                var origin = new Uri($"https://localhost:{Port()}");
                var broker = CreateApp(brokerOrigin.Port, certificate);
                var inspector = CreateApp(origin.Port, certificate);
                var pilot = new PilotSession(brokerOrigin, origin, certificate.GetCertHashString(HashAlgorithmName.SHA256));
                var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
                handler.ServerCertificateCustomValidationCallback = (_, cert, _, errors) => cert is not null && cert.GetCertHashString(HashAlgorithmName.SHA256) == certificate.GetCertHashString(HashAlgorithmName.SHA256) && (errors & ~System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) == 0;
                var http = new HttpClient(handler) { BaseAddress = origin, Timeout = TimeSpan.FromSeconds(35) };
                var fixture = new InspectorFixture(certificate, broker, inspector, pilot, http, origin, brokerOrigin);
                broker.MapPost("/mcp", async (HttpContext context) =>
                {
                    fixture.BrokerRequests++;
                    if (fixture.FailBroker) return Results.Text("fixture-credential-must-not-leak", statusCode: 500);
                    if (fixture.AcceptMcp)
                    {
                        using var rpc = await JsonDocument.ParseAsync(context.Request.Body);
                        if (!rpc.RootElement.TryGetProperty("id", out var id)) return Results.StatusCode(202);
                        var method = rpc.RootElement.GetProperty("method").GetString();
                        if (method == "server/discover") return Results.Json(new { jsonrpc = "2.0", id = id.Clone(), error = new { code = -32601, message = "Method not found" } }, statusCode: 404);
                        object result = method == "initialize"
                            ? new { protocolVersion = "2025-11-25", capabilities = new { tools = new { } }, serverInfo = new { name = "fixture", version = "1.0" } }
                            : new { content = new[] { new { type = "text", text = "{\"fixture\":true}" } } };
                        return Results.Json(new { jsonrpc = "2.0", id = id.Clone(), result });
                    }
                    context.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{brokerOrigin.AuthorityUri()}/.well-known/oauth-protected-resource/mcp\"";
                    return Results.StatusCode(401);
                });
                broker.MapGet("/.well-known/oauth-protected-resource/mcp", () => Results.Json(new { resource = brokerOrigin.AuthorityUri() + "/mcp", authorization_servers = new[] { brokerOrigin.AuthorityUri() }, scopes_supported = new[] { "kitos.read" } }));
                broker.MapGet("/.well-known/oauth-authorization-server", () => Results.Json(new { issuer = brokerOrigin.AuthorityUri(), authorization_endpoint = brokerOrigin.AuthorityUri() + "/authorize", token_endpoint = brokerOrigin.AuthorityUri() + "/connect/token", response_types_supported = new[] { "code" }, grant_types_supported = new[] { "authorization_code" }, code_challenge_methods_supported = new[] { "S256" }, token_endpoint_auth_methods_supported = new[] { "none" } }));
                broker.MapPost("/connect/token", () =>
                {
                    fixture.TokenExchangeRequests++;
                    return Results.StatusCode(500);
                });
                LocalClientHost.Map(inspector, origin, brokerOrigin, pilot);
                await broker.StartAsync();
                await inspector.StartAsync();
                return fixture;
            }

            internal async Task<string> LoadPage()
            {
                using var response = await _http.GetAsync("/");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                // Fetch's Origin-header algorithm nulls non-CORS form origins under no-referrer.
                // strict-origin preserves HTTPS same-origin POSTs without sharing callback code/path.
                var referrerPolicy = response.Headers.GetValues("Referrer-Policy").Single();
                BrowserPostOrigin = referrerPolicy == "no-referrer" ? "null" : Origin.AuthorityUri();
                Assert.Equal("strict-origin", referrerPolicy);
                Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
                var html = await response.Content.ReadAsStringAsync();
                return Regex.Match(html, "name=\"csrf\" value=\"([^\"]+)\"").Groups[1].Value;
            }

            internal Task<HttpResponseMessage> Post(string path, string? csrf, string origin)
            {
                var fields = new Dictionary<string, string>();
                if (csrf is not null) fields["csrf"] = csrf;
                var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(fields) };
                request.Headers.Add("Origin", origin);
                return SendAndDispose(request);
            }

            internal Task<HttpResponseMessage> GetPage() => _http.GetAsync("/");
            internal Task<HttpResponseMessage> Callback(string state) => _http.GetAsync("/callback?code=synthetic-code&state=" + Uri.EscapeDataString(state));

            private async Task<HttpResponseMessage> SendAndDispose(HttpRequestMessage request)
            {
                using (request) return await _http.SendAsync(request);
            }

            private static WebApplication CreateApp(int port, X509Certificate2 certificate)
            {
                var builder = WebApplication.CreateBuilder();
                builder.Logging.ClearProviders();
                builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, port, listener => listener.UseHttps(certificate)));
                return builder.Build();
            }

            private static int Port()
            {
                using var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }

            public async ValueTask DisposeAsync()
            {
                await _pilot.DisposeAsync();
                await _inspector.StopAsync(); await _inspector.DisposeAsync();
                await _broker.StopAsync(); await _broker.DisposeAsync();
                _http.Dispose(); _certificate.Dispose();
            }
        }
    }

    internal static class OriginExtensions
    {
        internal static string AuthorityUri(this Uri origin) => origin.GetLeftPart(UriPartial.Authority);
    }
}
