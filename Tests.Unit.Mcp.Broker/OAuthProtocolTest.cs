using Mcp.Broker;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tests.Unit.Mcp.Broker
{
    public class OAuthProtocolTest
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task AuthorizationCode_RealHttpsPkceExchange_DeniesReplayWrongClientAndMissingCsrf(bool dynamicRegistration, bool resourceOverride)
        {
            using var certificate = Certificate();
            var origin = new Uri($"https://localhost:{Port()}/");
            var pin = certificate.GetCertHashString(HashAlgorithmName.SHA256);
            var options = new BrokerOptions
            {
                PublicOrigin = origin,
                ReadPort = Port(),
                WorkerOrigin = new Uri("https://worker.invalid/"),
                KitosOrigin = new Uri("https://fixture.invalid/"),
                ClientId = "test-client",
                RedirectUri = dynamicRegistration ? "https://chatgpt.com/connector/oauth/TestCallback123" : "https://client.invalid/callback",
                EnableDynamicClientRegistration = dynamicRegistration,
                ResourceIdentifier = resourceOverride ? "https://tunnel-service.gateway.unified-0.internal.api.openai.org/v1/mcp/tunnel_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" : null,
                ServerCertificate = certificate,
                WorkloadCertificate = certificate,
                SigningCertificate = certificate,
                EncryptionCertificate = certificate,
                WorkerClientCertificateSha256 = pin,
                WorkerServerCertificateSha256 = pin
            };
            var kitosFixture = new FixtureKitos();
            using var upstream = new HttpClient(kitosFixture);
            using var worker = new FixtureWorker();
            using var workerHttp = new HttpClient(worker);
            await using var app = BrokerHost.Build([], options, builder =>
            {
                builder.Services.AddSingleton(new KitosClient(upstream, options.KitosOrigin, TimeProvider.System));
                builder.Services.AddSingleton(new BrokerHost.WorkerClient(workerHttp));
            });
            await app.StartAsync();
            using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
            handler.ServerCertificateCustomValidationCallback = (_, peer, _, errors) => peer is not null &&
                peer.GetCertHashString(HashAlgorithmName.SHA256) == pin && (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None;
            using var client = new HttpClient(handler) { BaseAddress = origin };
            var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
            var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            var registeredClient = options.ClientId;
            var uri = "/connect/authorize?client_id=" + registeredClient + "&redirect_uri=" + Uri.EscapeDataString(options.RedirectUri) +
                "&response_type=code&scope=kitos.read&resource=" + Uri.EscapeDataString(options.Resource) +
                "&state=test-state&code_challenge_method=S256&code_challenge=" + challenge;

            using var metadataResponse = await client.GetAsync("/.well-known/oauth-authorization-server");
            Assert.Equal(HttpStatusCode.OK, metadataResponse.StatusCode);
            using var metadata = JsonDocument.Parse(await metadataResponse.Content.ReadAsStringAsync());
            using var protectedMetadataResponse = await client.GetAsync("/.well-known/oauth-protected-resource/mcp");
            using var protectedMetadata = JsonDocument.Parse(await protectedMetadataResponse.Content.ReadAsStringAsync());
            Assert.Equal(options.ResourceIdentifier ?? new Uri(origin, "/mcp").AbsoluteUri, protectedMetadata.RootElement.GetProperty("resource").GetString());
            Assert.Equal(new[] { origin.AbsoluteUri }, protectedMetadata.RootElement.GetProperty("authorization_servers").EnumerateArray().Select(value => value.GetString()));
            Assert.Equal(new Uri(origin, "/connect/token").AbsoluteUri, metadata.RootElement.GetProperty("token_endpoint").GetString());
            Assert.Equal(new[] { "none" }, metadata.RootElement.GetProperty("token_endpoint_auth_methods_supported").EnumerateArray().Select(value => value.GetString()));
            Assert.Equal(new[] { "S256" }, metadata.RootElement.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(value => value.GetString()));
            if (dynamicRegistration)
            {
                Assert.Equal(new Uri(origin, "/connect/register").AbsoluteUri, metadata.RootElement.GetProperty("registration_endpoint").GetString());
                var body = JsonSerializer.Serialize(new { redirect_uris = new[] { options.RedirectUri }, token_endpoint_auth_method = "none", grant_types = new[] { "authorization_code" }, response_types = new[] { "code" }, scope = "kitos.read", client_name = "Fixture ChatGPT", client_uri = "https://display-only.invalid/" });
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    using var registration = await client.PostAsync("/connect/register", new StringContent(body, Encoding.UTF8, "application/json"));
                    Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
                    using var registered = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
                    registeredClient = registered.RootElement.GetProperty("client_id").GetString()!;
                    Assert.Equal(options.ClientId, registeredClient);
                    Assert.Equal(new[] { options.RedirectUri }, registered.RootElement.GetProperty("redirect_uris").EnumerateArray().Select(value => value.GetString()));
                    Assert.Equal("none", registered.RootElement.GetProperty("token_endpoint_auth_method").GetString());
                    Assert.False(registered.RootElement.TryGetProperty("client_secret", out _));
                }
                uri = uri.Replace("client_id=" + options.ClientId, "client_id=" + registeredClient, StringComparison.Ordinal);
            }
            else
            {
                Assert.False(metadata.RootElement.TryGetProperty("registration_endpoint", out _));
                using var unavailable = await client.PostAsync("/connect/register", new StringContent("{}", Encoding.UTF8, "application/json"));
                Assert.Equal(HttpStatusCode.NotFound, unavailable.StatusCode);
            }
            using var wrongClient = await client.GetAsync(uri.Replace("client_id=test-client", "client_id=wrong-client", StringComparison.Ordinal));
            Assert.Equal(HttpStatusCode.BadRequest, wrongClient.StatusCode);
            var callsBeforeWrongResource = kitosFixture.Calls;
            var deniedResource = resourceOverride ? new Uri(origin, "/mcp").AbsoluteUri : "https://other.invalid/mcp";
            using var wrongResource = await client.GetAsync(uri.Replace("resource=" + Uri.EscapeDataString(options.Resource), "resource=" + Uri.EscapeDataString(deniedResource), StringComparison.Ordinal));
            Assert.Equal(HttpStatusCode.BadRequest, wrongResource.StatusCode);
            Assert.Equal(callsBeforeWrongResource, kitosFixture.Calls);
            using var get = await client.GetAsync(uri);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            // Chrome applies form-action to redirects after submitting consent, including the OAuth callback.
            Assert.Equal("default-src 'none'; form-action 'self' " + new Uri(options.RedirectUri).GetLeftPart(UriPartial.Authority) + "; frame-ancestors 'none'",
                get.Headers.GetValues("Content-Security-Policy").Single());
            var html = await get.Content.ReadAsStringAsync();
            var fields = Regex.Matches(html, "name='([^']+)' value='([^']+)'").ToDictionary(match => WebUtility.HtmlDecode(match.Groups[1].Value), match => WebUtility.HtmlDecode(match.Groups[2].Value));
            fields["jwt"] = "fixture-token";
            fields["consent"] = "yes";
            var noCsrf = fields.Where(pair => pair.Key != "__RequestVerificationToken").ToDictionary(pair => pair.Key, pair => pair.Value);
            using var forbidden = await client.PostAsync("/connect/authorize", new FormUrlEncodedContent(noCsrf));
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            using var consent = await client.PostAsync("/connect/authorize", new FormUrlEncodedContent(fields));
            Assert.Equal(HttpStatusCode.Redirect, consent.StatusCode);
            var query = consent.Headers.Location!.Query.TrimStart('?').Split('&').Select(part => part.Split('=', 2)).ToDictionary(part => part[0], part => Uri.UnescapeDataString(part[1]));
            Assert.Equal("test-state", query["state"]);
            var exchange = new Dictionary<string, string> { ["grant_type"] = "authorization_code", ["client_id"] = options.ClientId, ["redirect_uri"] = options.RedirectUri, ["code"] = query["code"], ["code_verifier"] = "wrong-verifier", ["resource"] = options.Resource };
            if (resourceOverride)
            {
                var legacyExchange = new Dictionary<string, string>(exchange) { ["resource"] = new Uri(origin, "/mcp").AbsoluteUri, ["code_verifier"] = verifier };
                using var rejectedLegacy = await client.PostAsync("/connect/token", new FormUrlEncodedContent(legacyExchange));
                Assert.Equal(HttpStatusCode.BadRequest, rejectedLegacy.StatusCode);
            }
            using var wrongVerifier = await client.PostAsync("/connect/token", new FormUrlEncodedContent(exchange));
            Assert.Equal(HttpStatusCode.BadRequest, wrongVerifier.StatusCode);
            exchange["code_verifier"] = verifier;
            var races = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsync("/connect/token", new FormUrlEncodedContent(exchange))));
            try
            {
                Assert.Single(races, response => response.StatusCode == HttpStatusCode.OK);
                Assert.Equal(7, races.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
                var payload = await races.Single(response => response.StatusCode == HttpStatusCode.OK).Content.ReadAsStringAsync();
                Assert.DoesNotContain("fixture-token", payload);
                using var tokens = JsonDocument.Parse(payload);
                var access = tokens.RootElement.GetProperty("access_token").GetString();
                using var discovery = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
                discovery.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);
                discovery.Headers.Add("MCP-Protocol-Version", "2026-07-28");
                discovery.Headers.Add("Mcp-Method", "tools/call");
                discovery.Headers.Add("Mcp-Name", "get_domain_catalog");
                discovery.Headers.Add("X-Untrusted-Context", "must-not-forward");
                using var proxied = await client.SendAsync(discovery);
                Assert.Equal(HttpStatusCode.OK, proxied.StatusCode);
                Assert.Equal("tools/call", worker.Method);
                Assert.Equal("get_domain_catalog", worker.Name);
                Assert.Equal("2026-07-28", worker.Version);
                Assert.False(worker.AuthorizationForwarded);
                Assert.False(worker.UntrustedContextForwarded);
                using var revoke = new HttpRequestMessage(HttpMethod.Post, "/disconnect");
                revoke.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);
                using var revoked = await client.SendAsync(revoke);
                Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
                using var afterRevoke = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
                afterRevoke.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);
                using var denied = await client.SendAsync(afterRevoke);
                Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            }
            finally { foreach (var response in races) response.Dispose(); }
            await app.StopAsync();
        }

        [Fact]
        public async Task Listeners_PublicDoesNotRequestCertificates_PrivateRequiresPinnedWorkerAndRejectsOAuth()
        {
            using var server = Certificate();
            using var worker = Certificate();
            using var unrelated = Certificate();
            var options = new BrokerOptions
            {
                PublicOrigin = new Uri($"https://localhost:{Port()}/"),
                ReadPort = Port(),
                WorkerOrigin = new Uri("https://worker.invalid/"),
                KitosOrigin = new Uri("https://fixture.invalid/"),
                ClientId = "test-client",
                RedirectUri = "https://client.invalid/callback",
                ServerCertificate = server,
                WorkloadCertificate = server,
                SigningCertificate = server,
                EncryptionCertificate = server,
                WorkerClientCertificateSha256 = worker.GetCertHashString(HashAlgorithmName.SHA256),
                WorkerServerCertificateSha256 = server.GetCertHashString(HashAlgorithmName.SHA256)
            };
            await using var app = BrokerHost.Build([], options);
            await app.StartAsync();
            HttpClient Client(Uri origin, X509Certificate2? identity)
            {
                var handler = new HttpClientHandler { AllowAutoRedirect = false };
                handler.ServerCertificateCustomValidationCallback = (_, peer, _, errors) => peer is not null &&
                    peer.GetCertHashString(HashAlgorithmName.SHA256) == server.GetCertHashString(HashAlgorithmName.SHA256) &&
                    (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None;
                if (identity is not null) handler.ClientCertificates.Add(identity);
                return new HttpClient(handler) { BaseAddress = origin, Timeout = TimeSpan.FromSeconds(10) };
            }
            using var publicClient = Client(options.PublicOrigin, unrelated);
            using var metadata = await publicClient.GetAsync("/.well-known/oauth-authorization-server");
            Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
            using var publicRead = await publicClient.PostAsync("/reads", new StringContent("{}"));
            Assert.Equal(HttpStatusCode.Forbidden, publicRead.StatusCode);
            var readOrigin = new Uri($"https://localhost:{options.ReadPort}/");
            using var missingCertificate = Client(readOrigin, null);
            await Assert.ThrowsAsync<HttpRequestException>(() => missingCertificate.PostAsync("/reads", new StringContent("{}")));
            using var wrongCertificate = Client(readOrigin, unrelated);
            await Assert.ThrowsAsync<HttpRequestException>(() => wrongCertificate.PostAsync("/reads", new StringContent("{}")));
            using var workerClient = Client(readOrigin, worker);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/reads") { Content = new StringContent("{}") };
            request.Headers.Add("X-Mcp-Read-Grant", "unknown-grant");
            using var read = await workerClient.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
            using var privateOAuth = await workerClient.GetAsync("/.well-known/oauth-authorization-server");
            Assert.Equal(HttpStatusCode.Forbidden, privateOAuth.StatusCode);
            using var wrongMethod = await workerClient.GetAsync("/reads");
            Assert.Equal(HttpStatusCode.Forbidden, wrongMethod.StatusCode);
            await app.StopAsync();
        }

        [Fact]
        public async Task DynamicRegistration_RejectsWidenedOrMalformedMetadataAndKeepsSingletonPublicClient()
        {
            using var certificate = Certificate();
            var options = RegistrationOptions(certificate, "https://chatgpt.com/connector/oauth/TestCallback123");
            await using var app = BrokerHost.Build([], options);
            await app.StartAsync();
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            handler.ServerCertificateCustomValidationCallback = (_, peer, _, errors) => peer is not null &&
                peer.GetCertHashString(HashAlgorithmName.SHA256) == certificate.GetCertHashString(HashAlgorithmName.SHA256) &&
                (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None;
            using var client = new HttpClient(handler) { BaseAddress = options.PublicOrigin };
            var valid = "{\"redirect_uris\":[\"" + options.RedirectUri + "\"],\"token_endpoint_auth_method\":\"none\"}";
            var invalid = new[]
            {
                "{}", "[]", "{", valid.Replace("TestCallback123", "OtherCallback", StringComparison.Ordinal),
                valid.Replace(options.RedirectUri, options.RedirectUri + "?extra=1", StringComparison.Ordinal),
                valid.Replace("[\"" + options.RedirectUri + "\"]", "[\"" + options.RedirectUri + "\",\"https://attacker.invalid/\"]", StringComparison.Ordinal),
                valid.Replace("\"none\"", "\"client_secret_basic\"", StringComparison.Ordinal),
                valid.Replace("\"none\"", "null", StringComparison.Ordinal),
                valid.Replace(",\"token_endpoint_auth_method\":\"none\"", "", StringComparison.Ordinal),
                valid[..^1] + ",\"grant_types\":[\"authorization_code\",\"refresh_token\"]}",
                valid[..^1] + ",\"grant_types\":[\"client_credentials\"]}",
                valid[..^1] + ",\"response_types\":[\"token\"]}",
                valid[..^1] + ",\"scope\":\"kitos.read kitos.write\"}",
                valid[..^1] + ",\"client_secret\":\"must-not-echo\"}",
                valid[..^1] + ",\"jwks_uri\":\"https://must-not-fetch.invalid/\"}",
                valid[..^1] + ",\"jwks\":{}}", valid[..^1] + ",\"client_id\":\"attacker\"}",
                valid[..^1] + ",\"resource\":\"https://attacker.invalid/\"}",
                valid[..^1] + ",\"token_endpoint_auth_method\":\"none\"}"
            };
            foreach (var body in invalid)
            {
                using var response = await client.PostAsync("/connect/register", new StringContent(body, Encoding.UTF8, "application/json"));
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Equal("{\"error\":\"invalid_client_metadata\"}", await response.Content.ReadAsStringAsync());
            }
            using var oversized = await client.PostAsync("/connect/register", new StringContent(new string(' ', 4097), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);
            using var wrongContent = await client.PostAsync("/connect/register", new StringContent(valid, Encoding.UTF8, "text/plain"));
            Assert.Equal(HttpStatusCode.BadRequest, wrongContent.StatusCode);
            using var query = await client.PostAsync("/connect/register?client_id=attacker", new StringContent(valid, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, query.StatusCode);
            using var first = await client.PostAsync("/connect/register", new StringContent(valid, Encoding.UTF8, "application/json"));
            using var second = await client.PostAsync("/connect/register", new StringContent(valid, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
            using var registered = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
            Assert.Equal(options.ClientId, registered.RootElement.GetProperty("client_id").GetString());
            Assert.Equal("kitos.read", registered.RootElement.GetProperty("scope").GetString());
            Assert.Equal(new[] { "authorization_code" }, registered.RootElement.GetProperty("grant_types").EnumerateArray().Select(value => value.GetString()));
            using var privateHandler = new HttpClientHandler();
            privateHandler.ClientCertificates.Add(certificate);
            privateHandler.ServerCertificateCustomValidationCallback = handler.ServerCertificateCustomValidationCallback;
            using var privateClient = new HttpClient(privateHandler) { BaseAddress = new Uri($"https://localhost:{options.ReadPort}/") };
            using var privateRegistration = await privateClient.PostAsync("/connect/register", new StringContent(valid, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Forbidden, privateRegistration.StatusCode);
            await app.StopAsync();
        }

        [Theory]
        [InlineData("https://localhost:7445/callback")]
        [InlineData("https://attacker.invalid/connector/oauth/TestCallback")]
        [InlineData("https://chatgpt.com:444/connector/oauth/TestCallback")]
        [InlineData("https://chatgpt.com/connector/oauth/")]
        [InlineData("https://chatgpt.com/connector/oauth/Test/Callback")]
        [InlineData("https://chatgpt.com/connector/oauth/TestCallback?extra=1")]
        [InlineData("https://chatgpt.com/other/TestCallback")]
        [InlineData("https://chatgpt.com/connector/oauth/Test%43allback")]
        public void DynamicRegistration_RejectsConfigurationOutsideExactChatGptCallback(string callback)
        {
            using var certificate = Certificate();
            Assert.Throws<InvalidOperationException>(() => RegistrationOptions(certificate, callback).Validate());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("/mcp")]
        [InlineData("http://resource.invalid/mcp")]
        [InlineData("https://user:password@resource.invalid/mcp")]
        [InlineData("https://resource.invalid/mcp?query=1")]
        [InlineData("https://resource.invalid/mcp#fragment")]
        [InlineData("https://resource.invalid/space in identifier")]
        public void OAuthResourceIdentifier_RejectsUnsafeExplicitConfiguration(string identifier)
        {
            using var certificate = Certificate();
            Assert.Throws<InvalidOperationException>(() => RegistrationOptions(certificate, "https://chatgpt.com/connector/oauth/TestCallback123", identifier).Validate());
        }

        [Fact]
        public void OAuthResourceIdentifier_IsAnExactIdentifierAndDoesNotChangeIssuerOrListener()
        {
            using var certificate = Certificate();
            const string identifier = "https://resource.invalid/exact%2Fidentifier";
            var options = RegistrationOptions(certificate, "https://chatgpt.com/connector/oauth/TestCallback123", identifier);
            options.Validate();
            Assert.Equal(identifier, options.Resource);
            Assert.Equal("localhost", options.PublicOrigin.Host);
            Assert.NotEqual(new Uri(identifier).Port, options.PublicOrigin.Port);
        }

        private static BrokerOptions RegistrationOptions(X509Certificate2 certificate, string callback, string? resourceIdentifier = null) => new()
        {
            PublicOrigin = new Uri($"https://localhost:{Port()}/"),
            ReadPort = Port(),
            WorkerOrigin = new Uri("https://worker.invalid/"),
            KitosOrigin = new Uri("https://fixture.invalid/"),
            ClientId = "test-client",
            RedirectUri = callback,
            EnableDynamicClientRegistration = true,
            ResourceIdentifier = resourceIdentifier,
            ServerCertificate = certificate,
            WorkloadCertificate = certificate,
            SigningCertificate = certificate,
            EncryptionCertificate = certificate,
            WorkerClientCertificateSha256 = certificate.GetCertHashString(HashAlgorithmName.SHA256),
            WorkerServerCertificateSha256 = certificate.GetCertHashString(HashAlgorithmName.SHA256)
        };

        private static int Port()
        {
            var socket = new TcpListener(IPAddress.Loopback, 0);
            socket.Start();
            var port = ((IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
            return port;
        }
        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        private static X509Certificate2 Certificate()
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            // Schannel requires a Windows user key container for HTTPS fixtures; disposal removes this temporary key.
            return X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
        }
        private sealed class FixtureWorker : HttpMessageHandler
        {
            internal string? Method { get; private set; }
            internal string? Name { get; private set; }
            internal string? Version { get; private set; }
            internal bool AuthorizationForwarded { get; private set; }
            internal bool UntrustedContextForwarded { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Method = request.Headers.TryGetValues("Mcp-Method", out var method) ? method.Single() : null;
                Name = request.Headers.TryGetValues("Mcp-Name", out var name) ? name.Single() : null;
                Version = request.Headers.TryGetValues("MCP-Protocol-Version", out var version) ? version.Single() : null;
                AuthorizationForwarded = request.Headers.Contains("Authorization");
                UntrustedContextForwarded = request.Headers.Contains("X-Untrusted-Context");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
            }
        }

        private sealed class FixtureKitos : HttpMessageHandler
        {
            internal int Calls { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                var body = request.Method == HttpMethod.Post ? JsonSerializer.Serialize(new { active = true, expiration = DateTimeOffset.UtcNow.AddMinutes(10), claims = new[] { new { type = "name", value = "42" } } }) : "[]";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
            }
        }
    }
}
