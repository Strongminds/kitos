using Mcp.Worker;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net;
using System.Security.Cryptography;
using Xunit;

namespace Tests.Unit.Mcp.Worker
{
    public class WorkerProtocolTest
    {
        [Fact]
        public async Task HttpsMcp_TrustedPeer_DiscoversQueriesAndRejectsUnsupportedPlansBeforeRead()
        {
            using var serverCertificate = WorkerBehaviorTest.Certificate("localhost");
            using var clientCertificate = WorkerBehaviorTest.Certificate("trusted-peer");
            using var stub = new WorkerBehaviorTest.BrokerStub(HttpStatusCode.OK, WorkerBehaviorTest.Envelope);
            await using var app = WorkerHost.Create([], builder =>
            {
                builder.Logging.ClearProviders();
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Worker:BrokerOrigin"] = "https://broker.example/",
                    ["Worker:BrokerClientCertificateSha256"] = clientCertificate.GetCertHashString(HashAlgorithmName.SHA256),
                    ["Worker:BrokerServerCertificateSha256"] = serverCertificate.GetCertHashString(HashAlgorithmName.SHA256),
                    ["Worker:ClientCertificatePath"] = "unused-by-stub.pfx"
                });
                builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(https =>
                {
                    https.ServerCertificate = serverCertificate;
                    https.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
                    https.ClientCertificateValidation = (cert, _, _) => PeerCertificate.Matches(cert, clientCertificate.GetCertHashString(HashAlgorithmName.SHA256));
                })));
                builder.Services.PostConfigure<HttpClientFactoryOptions>(nameof(BoundaryClient), options =>
                {
                    options.HttpMessageHandlerBuilderActions.Clear();
                    options.HttpMessageHandlerBuilderActions.Add(handler => handler.PrimaryHandler = stub);
                });
            });
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single().Replace("127.0.0.1", "localhost", StringComparison.Ordinal);
                using var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(clientCertificate);
                handler.ServerCertificateCustomValidationCallback = (_, cert, _, errors) => PeerCertificate.MatchesServer(cert, serverCertificate.GetCertHashString(HashAlgorithmName.SHA256), errors);
                using var http = new HttpClient(handler);
                await using var transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(address + "/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp,
                    AdditionalHeaders = new Dictionary<string, string> { ["X-Mcp-Read-Grant"] = WorkerBehaviorTest.Grant }
                }, http);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await using var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
                var tools = await client.ListToolsAsync(cancellationToken: timeout.Token);
                Assert.Equal(new[] { "get_domain_catalog", "query_kitos" }, tools.Select(t => t.Name).Order().ToArray());
                var catalog = await client.CallToolAsync("get_domain_catalog", cancellationToken: timeout.Token);
                Assert.NotEqual(true, catalog.IsError);
                Assert.Contains("systems_name_query_single_page", Assert.IsType<TextContentBlock>(Assert.Single(catalog.Content)).Text);
                Assert.Equal(0, stub.Calls);
                var rejected = await client.CallToolAsync("query_kitos", new Dictionary<string, object?> { ["plan"] = WorkerBehaviorTest.Plan.Replace("\"contains\"", "\"eq\"", StringComparison.Ordinal) }, cancellationToken: timeout.Token);
                Assert.True(rejected.IsError);
                Assert.Equal(0, stub.Calls);
                var result = await client.CallToolAsync("query_kitos", new Dictionary<string, object?> { ["plan"] = WorkerBehaviorTest.Plan }, cancellationToken: timeout.Token);
                Assert.False(result.IsError ?? false, string.Join(" ", result.Content.OfType<TextContentBlock>().Select(block => block.Text)));
                Assert.Contains("caller_visible", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
                Assert.Equal(1, stub.Calls);
                using var missingGrant = new HttpRequestMessage(HttpMethod.Post, address + "/mcp");
                missingGrant.Content = new StringContent("{}");
                using var denied = await http.SendAsync(missingGrant, timeout.Token);
                Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
                Assert.Equal(1, stub.Calls);
                using var untrustedHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, cert, _, errors) => PeerCertificate.MatchesServer(cert, serverCertificate.GetCertHashString(HashAlgorithmName.SHA256), errors)
                };
                using var untrusted = new HttpClient(untrustedHandler);
                using var noPeerRequest = new HttpRequestMessage(HttpMethod.Post, address + "/mcp") { Content = new StringContent("{}") };
                noPeerRequest.Headers.Add("X-Mcp-Read-Grant", WorkerBehaviorTest.Grant);
                await Assert.ThrowsAsync<HttpRequestException>(() => untrusted.SendAsync(noPeerRequest, timeout.Token));
                Assert.Equal(1, stub.Calls);
                using var wrongCertificate = WorkerBehaviorTest.Certificate("wrong-peer");
                using var wrongHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, cert, _, errors) => PeerCertificate.MatchesServer(cert, serverCertificate.GetCertHashString(HashAlgorithmName.SHA256), errors)
                };
                wrongHandler.ClientCertificates.Add(wrongCertificate);
                using var wrongPeer = new HttpClient(wrongHandler);
                using var wrongPeerRequest = new HttpRequestMessage(HttpMethod.Post, address + "/mcp") { Content = new StringContent("{}") };
                wrongPeerRequest.Headers.Add("X-Mcp-Read-Grant", WorkerBehaviorTest.Grant);
                await Assert.ThrowsAsync<HttpRequestException>(() => wrongPeer.SendAsync(wrongPeerRequest, timeout.Token));
                Assert.Equal(1, stub.Calls);
                using var browserRequest = new HttpRequestMessage(HttpMethod.Post, address + "/mcp") { Content = new StringContent("{}") };
                browserRequest.Headers.Add("X-Mcp-Read-Grant", WorkerBehaviorTest.Grant);
                browserRequest.Headers.Add("Origin", "https://untrusted.example");
                using var browserDenied = await http.SendAsync(browserRequest, timeout.Token);
                Assert.Equal(HttpStatusCode.BadRequest, browserDenied.StatusCode);
            }
            finally { await app.StopAsync(); }
        }
    }
}
