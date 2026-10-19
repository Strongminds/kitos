using Core.Abstractions.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Mcp.ReadBoundary.Core
{
    public sealed class ValidatedReadRequest
    {
        internal ValidatedReadRequest(Uri requestUri)
        {
            RequestUri = requestUri;
        }

        public string Operation => "systems.list";
        public string Method => "GET";
        public Uri RequestUri { get; }
    }

    public sealed class ReadRequestValidationError : OperationError
    {
        public ReadRequestValidationError(string code, string path) : base($"{code} at {path}", OperationFailure.BadInput)
        {
            Code = code;
            Path = path;
        }

        public string Code { get; }
        public string Path { get; }
    }

    public sealed class ReadRequestPolicy
    {
        public const int MaxJsonBytes = 4096;
        public const int MaxNameLength = 256;
        public const int MaxPage = 999;
        public const int MaxPageSize = 250;
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private readonly Uri _destination;

        public ReadRequestPolicy(Uri configuredHttpsOrigin)
        {
            ArgumentNullException.ThrowIfNull(configuredHttpsOrigin);
            if (!configuredHttpsOrigin.IsAbsoluteUri || configuredHttpsOrigin.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrEmpty(configuredHttpsOrigin.Host) || configuredHttpsOrigin.AbsolutePath != "/" ||
                configuredHttpsOrigin.UserInfo.Length != 0 || configuredHttpsOrigin.OriginalString.Contains('@') ||
                configuredHttpsOrigin.Query.Length != 0 || configuredHttpsOrigin.Fragment.Length != 0 ||
                !HasLiteralRootPath(configuredHttpsOrigin.OriginalString))
            {
                throw new ArgumentException("The read boundary requires an absolute HTTPS origin with root path and no user information, query, or fragment.", nameof(configuredHttpsOrigin));
            }
            _destination = new Uri(configuredHttpsOrigin, "/api/v2/it-systems");
        }

        public Result<ValidatedReadRequest, OperationError> ValidateJson(string? json)
        {
            if (json is null) return Failure("invalid_json", "$");
            if (json.Length > MaxJsonBytes) return Failure("request_size", "$");
            try
            {
                if (StrictUtf8.GetByteCount(json) > MaxJsonBytes) return Failure("request_size", "$");
                using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
                var root = document.RootElement;
                if (!HasOnlyMembers(root, "operation", "parameters") ||
                    !root.TryGetProperty("operation", out var operation) || operation.ValueKind != JsonValueKind.String ||
                    !root.TryGetProperty("parameters", out var parameters) ||
                    !HasOnlyMembers(parameters, "page", "pageSize", "nameContains"))
                {
                    return Failure("invalid_json", "$");
                }
                if (operation.GetString() != "systems.list") return Failure("unsupported_operation", "operation");

                var page = 0;
                if (parameters.TryGetProperty("page", out var pageValue) &&
                    !IsIntegerInRange(pageValue, 0, MaxPage, out page))
                    return Failure("invalid_parameter", "parameters.page");
                var pageSize = 100;
                if (parameters.TryGetProperty("pageSize", out var pageSizeValue) &&
                    !IsIntegerInRange(pageSizeValue, 1, MaxPageSize, out pageSize))
                    return Failure("invalid_parameter", "parameters.pageSize");

                var query = $"page={page.ToString(CultureInfo.InvariantCulture)}&pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}";
                if (parameters.TryGetProperty("nameContains", out var nameValue))
                {
                    if (nameValue.ValueKind != JsonValueKind.String) return Failure("invalid_parameter", "parameters.nameContains");
                    var name = nameValue.GetString();
                    if (name is null || name.Length > MaxNameLength) return Failure("invalid_parameter", "parameters.nameContains");
                    query += "&nameContains=" + Uri.EscapeDataString(name);
                }

                return new ValidatedReadRequest(new Uri(_destination.AbsoluteUri + "?" + query));
            }
            catch (JsonException)
            {
                return Failure("invalid_json", "$");
            }
            catch (InvalidOperationException)
            {
                // JsonDocument may parse escaped unpaired surrogates; decoding their strings fails here.
                return Failure("invalid_json", "$");
            }
            catch (EncoderFallbackException)
            {
                return Failure("invalid_json", "$");
            }
        }

        private static bool HasLiteralRootPath(string origin)
        {
            var authorityStart = origin.IndexOf("://", StringComparison.Ordinal);
            if (authorityStart < 0) return false;
            var pathStart = origin.IndexOfAny(['/', '\\', '?', '#'], authorityStart + 3);
            return pathStart < 0 || origin[pathStart..] == "/";
        }

        private static bool HasOnlyMembers(JsonElement value, params string[] allowed)
        {
            if (value.ValueKind != JsonValueKind.Object) return false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name) || Array.IndexOf(allowed, property.Name) < 0) return false;
            }
            return true;
        }

        private static bool IsIntegerInRange(JsonElement value, int minimum, int maximum, out int number)
        {
            number = 0;
            return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out number) && number >= minimum && number <= maximum;
        }

        private static Result<ValidatedReadRequest, OperationError> Failure(string code, string path) =>
            new ReadRequestValidationError(code, path);
    }
}
