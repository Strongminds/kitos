using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text.Json;

namespace Tools.Mcp.LocalClient
{
    /// <summary>Single local inspection session. SDK owns OAuth state, PKCE, token caching and token exchange.</summary>
    public sealed class PilotSession : IAsyncDisposable
    {
        private readonly Uri _broker;
        private readonly Uri _local;
        private readonly HttpClient _http;
        private readonly SemaphoreSlim _gate = new(1);
        private TaskCompletionSource<Uri?>? _authorization;
        private TaskCompletionSource<AuthorizationResult>? _callback;
        private McpClient? _client;
        private HttpClientTransport? _transport;
        private CancellationTokenSource? _connectionCancellation;
        private Task? _connection;
        private string _connectionStage = "connection startup";
        private int? _lastHttpStatus;
        public string Status { get; private set; } = "Disconnected";
        public string? Result { get; private set; }

        public PilotSession(Uri broker, Uri local, string brokerCertificatePin)
        {
            _broker = broker;
            _local = local;
            var tls = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
            tls.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) => certificate is not null &&
                (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == 0 &&
                certificate.NotBefore.ToUniversalTime() <= DateTime.UtcNow && certificate.NotAfter.ToUniversalTime() > DateTime.UtcNow &&
                CryptographicOperations.FixedTimeEquals(certificate.GetCertHash(HashAlgorithmName.SHA256), Convert.FromHexString(brokerCertificatePin));
            _http = new HttpClient(new FixedOriginHandler(broker, (stage, status) =>
            {
                _connectionStage = stage;
                _lastHttpStatus = status;
            })
            { InnerHandler = tls })
            { Timeout = TimeSpan.FromSeconds(20) };
        }

        public async Task<Uri?> ConnectAsync()
        {
            Task<Uri?> authorization;
            await _gate.WaitAsync();
            try
            {
                if (_client is null && _connection is { IsCompleted: false })
                {
                    if (_callback?.Task.IsCompleted == true) return null;
                    authorization = _authorization!.Task;
                }
                else
                {
                    await ClearConnectionAsync();
                    _connectionStage = "connection startup";
                    _lastHttpStatus = null;
                    _authorization = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    _callback = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    _connectionCancellation = new(TimeSpan.FromMinutes(5));
                    Status = "Connecting";
                    _connection = RunConnectionAsync(_connectionCancellation.Token);
                    authorization = _authorization.Task;
                }
            }
            finally { _gate.Release(); }
            try { return await authorization.WaitAsync(TimeSpan.FromSeconds(25)); }
            catch (TimeoutException)
            {
                _connectionCancellation?.Cancel();
                Status = "Connection could not start. Verify the local services and try Connect again.";
                return null;
            }
        }

        private async Task RunConnectionAsync(CancellationToken cancellationToken)
        {
            McpClient? candidate = null;
            try
            {
                _transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(_broker, "/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp,
                    OAuth = new ClientOAuthOptions
                    {
                        ClientId = "kitos-local-pilot",
                        RedirectUri = new Uri(_local, "/callback"),
                        Scopes = ["kitos.read"],
                        AuthorizationCallbackHandler = async (context, token) =>
                        {
                            if (context.RedirectUri != new Uri(_local, "/callback") || context.AuthorizationUri.GetLeftPart(UriPartial.Authority) != _broker.GetLeftPart(UriPartial.Authority))
                                throw new InvalidOperationException("Unexpected OAuth destination.");
                            _authorization!.TrySetResult(context.AuthorizationUri);
                            _connectionStage = "authorization callback";
                            _lastHttpStatus = null;
                            return await _callback!.Task.WaitAsync(token);
                        }
                    }
                }, _http);
                // Interactive OAuth must share the host's finite authorization budget, not the SDK's five-second discovery probe.
                candidate = await McpClient.CreateAsync(_transport, new McpClientOptions
                {
                    DiscoverProbeTimeout = Timeout.InfiniteTimeSpan,
                    InitializationTimeout = TimeSpan.FromMinutes(5)
                }, cancellationToken: cancellationToken);
                var catalog = await candidate.CallToolAsync("get_domain_catalog", cancellationToken: cancellationToken);
                Result = string.Join("\n", catalog.Content.OfType<TextContentBlock>().Select(block => block.Text));
                _client = candidate;
                candidate = null;
                Status = "Connected. Query a system name below.";
                _authorization!.TrySetResult(null);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                // Exception messages and protocol bodies can contain credentials; show only trusted stage labels/status codes.
                Status = $"Connection failed during {_connectionStage}{(_lastHttpStatus is { } code ? $" (HTTP {code})" : string.Empty)}. Try Connect again.";
                _authorization!.TrySetResult(null);
            }
            finally { if (candidate is not null) await candidate.DisposeAsync(); }
        }

        public bool CompleteAuthorization(IQueryCollection query)
        {
            if (_callback is null || _callback.Task.IsCompleted || _connectionCancellation?.IsCancellationRequested != false || query["code"].Count != 1 || query["state"].Count != 1 || query["iss"].Count > 1 ||
                string.IsNullOrEmpty(query["code"]) || string.IsNullOrEmpty(query["state"])) return false;
            return _callback.TrySetResult(new AuthorizationResult { Code = query["code"]!, State = query["state"]!, Iss = query["iss"].FirstOrDefault() });
        }

        public async Task QueryAsync(string name, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (_client is null || string.IsNullOrWhiteSpace(name) || name.Length > 256) { Status = "Connect first and enter a system name of up to 256 characters."; return; }
                using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                bounded.CancelAfter(TimeSpan.FromSeconds(30));
                var plan = JsonSerializer.Serialize(new { catalogVersion = "1.0.0", entity = "systems", projection = new[] { "uuid", "name" }, filters = new[] { new { field = "name", @operator = "contains", value = name } }, traversal = Array.Empty<string>() });
                var result = await _client.CallToolAsync("query_kitos", new Dictionary<string, object?> { ["plan"] = plan }, cancellationToken: bounded.Token);
                Result = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));
                Status = result.IsError == true ? "Query incomplete; inspect the result." : "Query returned. Coverage and completeness are in the result.";
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                await ClearConnectionAsync();
                Status = "Query failed or connection expired. Connect again to authorize.";
            }
            finally { _gate.Release(); }
        }

        private async Task ClearConnectionAsync()
        {
            _connectionCancellation?.Cancel();
            if (_connection is not null) await _connection;
            var client = _client;
            var transport = _transport;
            _client = null;
            _transport = null;
            _connection = null;
            _callback = null;
            _authorization = null;
            Result = null;
            Status = "Disconnected";
            await DisposePeer(client);
            await DisposePeer(transport);
            _connectionCancellation?.Dispose();
            _connectionCancellation = null;

            static async Task DisposePeer(IAsyncDisposable? peer)
            {
                if (peer is null) return;
                try { await peer.DisposeAsync(); }
                // An expired/offline peer must not prevent clearing local state and authorizing afresh.
                catch (Exception error) when (error is HttpRequestException or OperationCanceledException or IOException) { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await ClearConnectionAsync();
            _http.Dispose();
            _gate.Dispose();
        }

        private sealed class FixedOriginHandler(Uri broker, Action<string, int?> observe) : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri is not { Scheme: "https" } destination || destination.GetLeftPart(UriPartial.Authority) != broker.GetLeftPart(UriPartial.Authority))
                    throw new HttpRequestException("Unexpected broker destination.");
                var stage = destination.AbsolutePath switch
                {
                    "/connect/token" => "OAuth token exchange",
                    "/mcp" => "MCP connection",
                    _ => "OAuth discovery"
                };
                observe(stage, null);
                var response = await base.SendAsync(request, cancellationToken);
                observe(stage, (int)response.StatusCode);
                return response;
            }
        }
    }
}
