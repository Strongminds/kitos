using Core.Abstractions.Types;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mcp.Worker
{
    public sealed class BoundaryClient(HttpClient client)
    {
        private const int MaxResponseBytes = 2 * 1024 * 1024;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static bool IsGrant(string? value) => value is { Length: >= 32 and <= 128 } &&
            value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

        public async Task<Result<JsonElement, OperationError>> ReadAsync(SystemListRequest operation, string grant, CancellationToken cancellationToken)
        {
            if (!IsGrant(grant)) return Failure("authorization_required", OperationFailure.Forbidden);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/reads") { Content = JsonContent.Create(operation, options: JsonOptions) };
            request.Headers.Add("X-Mcp-Read-Grant", grant);
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(12));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode)
                    return response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => Failure("authorization_required", OperationFailure.Forbidden),
                        HttpStatusCode.TooManyRequests => Failure("read_budget_exceeded", OperationFailure.Conflict),
                        _ => Failure("read_incomplete", OperationFailure.UnknownError)
                    };
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var buffer = new MemoryStream();
                var bytes = new byte[8192];
                while (true)
                {
                    var count = await stream.ReadAsync(bytes, timeout.Token);
                    if (count == 0) break;
                    if (buffer.Length + count > MaxResponseBytes) return Failure("read_incomplete", OperationFailure.UnknownError);
                    await buffer.WriteAsync(bytes.AsMemory(0, count), timeout.Token);
                }
                using var document = JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array ||
                    !root.TryGetProperty("coverage", out var coverage) || coverage.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("catalogVersion", out var version) || version.ValueKind != JsonValueKind.String || version.GetString() != "1.0.0" ||
                    !IsValidResponse(data, coverage))
                    return Failure("incompatible_response", OperationFailure.UnknownError);
                return root.Clone();
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException or IOException)
            {
                // Dependency bodies and exception messages may contain credentials. Never return them to a tool.
                return Failure("read_incomplete", OperationFailure.UnknownError);
            }
        }

        private static bool IsValidResponse(JsonElement data, JsonElement coverage)
        {
            if (data.GetArrayLength() > 100 || coverage.EnumerateObject().Count() != 8) return false;
            foreach (var item in data.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 2 ||
                    !item.TryGetProperty("uuid", out var uuid) || uuid.ValueKind != JsonValueKind.String || !uuid.TryGetGuid(out var id) || id == Guid.Empty ||
                    !item.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String || name.GetString() is not { Length: > 0 and <= 4096 }) return false;
            }
            return HasText(coverage, "scope", "caller_visible") && HasText(coverage, "execution", "single_page") &&
                HasText(coverage, "consistency", "best_effort") && HasInteger(coverage, "page", 0) && HasInteger(coverage, "pageSize", 100) &&
                coverage.TryGetProperty("mayHaveMore", out var more) && more.ValueKind is JsonValueKind.True or JsonValueKind.False &&
                coverage.TryGetProperty("startedAt", out var start) && start.ValueKind == JsonValueKind.String && start.TryGetDateTimeOffset(out var startedAt) &&
                coverage.TryGetProperty("completedAt", out var end) && end.ValueKind == JsonValueKind.String && end.TryGetDateTimeOffset(out var completedAt) && completedAt >= startedAt;
        }

        private static bool HasText(JsonElement value, string property, string expected) => value.TryGetProperty(property, out var field) &&
            field.ValueKind == JsonValueKind.String && field.GetString() == expected;
        private static bool HasInteger(JsonElement value, string property, int expected) => value.TryGetProperty(property, out var field) &&
            field.ValueKind == JsonValueKind.Number && field.TryGetInt32(out var number) && number == expected;

        private static Result<JsonElement, OperationError> Failure(string code, OperationFailure failure) => new OperationError(code, failure);
    }
}
