using Mcp.ReadBoundary.Core;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Mcp.Broker
{
    internal sealed class KitosClient(HttpClient client, Uri origin, TimeProvider time, PilotState? state = null)
    {
        private readonly SemaphoreSlim _loginSlots = new(16, 16);

        internal async Task<(string User, DateTimeOffset Expires)?> Authenticate(string jwt, CancellationToken cancellation)
        {
            if (!await _loginSlots.WaitAsync(0, cancellation)) return null;
            try { return await AuthenticateCore(jwt, cancellation); }
            finally { _loginSlots.Release(); }
        }
        private async Task<(string User, DateTimeOffset Expires)?> AuthenticateCore(string jwt, CancellationToken cancellation)
        {
            if (jwt.Length is < 1 or > 8192 || jwt.Any(char.IsWhiteSpace)) return null;
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(origin, "/api/v2/token/validate"));
            request.Content = JsonContent.Create(new { token = jwt });
            using var document = await Send(request, cancellation);
            if (document is null) return null;
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("active", out var active) || active.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("expiration", out var expiration) || expiration.ValueKind != JsonValueKind.String || !expiration.TryGetDateTimeOffset(out var expiry) || expiry <= time.GetUtcNow() ||
                !root.TryGetProperty("claims", out var claims) || claims.ValueKind != JsonValueKind.Array) return null;
            var names = claims.EnumerateArray().Where(claim => claim.ValueKind == JsonValueKind.Object && claim.TryGetProperty("type", out var type) &&
                type.ValueKind == JsonValueKind.String && type.GetString() is ClaimTypes.Name or "name" or "unique_name").ToArray();
            if (names.Length != 1 || !names[0].TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.String ||
                !int.TryParse(value.GetString(), out var user) || user <= 0) return null;
            using var admission = state?.Admit(user.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (state is not null && admission is null) return null;
            using var probe = new HttpRequestMessage(HttpMethod.Get, new Uri(origin, "/api/v2/it-systems?page=0&pageSize=1"));
            probe.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var probeResult = await Send(probe, cancellation);
            return probeResult?.RootElement.ValueKind == JsonValueKind.Array ? (user.ToString(System.Globalization.CultureInfo.InvariantCulture), expiry) : null;
        }

        internal async Task<object?> Read(ValidatedReadRequest validated, Credential credential, CancellationToken cancellation)
        {
            var start = time.GetUtcNow();
            using var request = new HttpRequestMessage(HttpMethod.Get, validated.RequestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential.Jwt);
            using var document = await Send(request, cancellation);
            if (document?.RootElement.ValueKind != JsonValueKind.Array) return null;
            var data = new List<object>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (data.Count >= 100 || item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("uuid", out var uuid) || uuid.ValueKind != JsonValueKind.String || !uuid.TryGetGuid(out var identity) ||
                    identity == Guid.Empty || !item.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String || name.GetString() is not { } text || string.IsNullOrWhiteSpace(text) || text.Length > 4096) return null;
                data.Add(new { uuid = identity, name = text });
            }
            // W01 deliberately permits only the first page/default size, rather than claiming full retrieval.
            return new { data, coverage = new { scope = "caller_visible", execution = "single_page", consistency = "best_effort", startedAt = start, completedAt = time.GetUtcNow(), page = 0, pageSize = 100, mayHaveMore = data.Count == 100 }, catalogVersion = "1.0.0" };
        }

        private async Task<JsonDocument?> Send(HttpRequestMessage request, CancellationToken cancellation)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > 2 * 1024 * 1024) return null;
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var buffer = new MemoryStream();
                var chunk = new byte[8192];
                int read;
                while ((read = await stream.ReadAsync(chunk, timeout.Token)) != 0)
                {
                    if (buffer.Length + read > 2 * 1024 * 1024) return null;
                    buffer.Write(chunk, 0, read);
                }
                return JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            }
            catch (Exception error) when (error is HttpRequestException or OperationCanceledException or JsonException or IOException) { return null; }
        }
    }
}
