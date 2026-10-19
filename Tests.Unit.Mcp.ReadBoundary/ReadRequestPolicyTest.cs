using Core.Abstractions.Types;
using Mcp.ReadBoundary.Core;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tests.Unit.Mcp.ReadBoundary
{
    public class ReadRequestPolicyTest
    {
        [Fact]
        public void ValidateJson_ValidOperation_ComposesFixedGetRequestWithDefaults()
        {
            var result = new ReadRequestPolicy(new Uri("https://kitos.example")).ValidateJson("""
                {"operation":"systems.list","parameters":{}}
                """);
            Assert.True(result.Ok);
            Assert.Equal("GET", result.Value.Method);
            Assert.Equal("https://kitos.example/api/v2/it-systems?page=0&pageSize=100", result.Value.RequestUri.AbsoluteUri);
        }

        [Fact]
        public void ValidateJson_ArbitraryRoute_IsRejected()
        {
            var result = new ReadRequestPolicy(new Uri("https://kitos.example")).ValidateJson("""
                {"operation":"systems.list","parameters":{},"url":"https://attacker.example"}
                """);
            Assert.True(result.Failed);
            Assert.Equal("invalid_json", Assert.IsType<ReadRequestValidationError>(result.Error).Code);
        }

        [Theory]
        [InlineData("{")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("true")]
        [InlineData("{}")]
        [InlineData("{\"operation\":\"systems.list\"}")]
        [InlineData("{\"parameters\":{}}")]
        [InlineData("{\"operation\":null,\"parameters\":{}}")]
        [InlineData("{\"operation\":7,\"parameters\":{}}")]
        [InlineData("{\"operation\":\"systems.list\",\"parameters\":null}")]
        [InlineData("{\"operation\":\"systems.list\",\"parameters\":[]}")]
        [InlineData("{\"operation\":\"systems.list\",\"parameters\":{},\"operation\":\"systems.list\"}")]
        [InlineData("{\"operation\":\"systems.list\",\"parameters\":{},\"\\u006fperation\":\"systems.list\"}")]
        [InlineData("{\"operation\":\"systems.list\",\"parameters\":{\"page\":0,\"page\":1}}")]
        [InlineData("{\"Operation\":\"systems.list\",\"parameters\":{}}")]
        [InlineData("{\"operation\":\"systems.list\",\"parameters\":{\"Page\":0}}")]
        public void ValidateJson_MalformedOrUnsupportedShape_ReturnsSafeBadInput(string json)
        {
            AssertFailure(Policy().ValidateJson(json), "invalid_json", "$");
        }

        [Theory]
        [InlineData("systems.get")]
        [InlineData("Systems.List")]
        [InlineData("systems.list ")]
        [InlineData("POST")]
        [InlineData("https://attacker.example/api/v2/it-systems")]
        public void ValidateJson_UnsupportedOperation_ReturnsSafeBadInput(string operation)
        {
            AssertFailure(Policy().ValidateJson(JsonSerializer.Serialize(new { operation, parameters = new { } })),
                "unsupported_operation", "operation");
        }

        [Theory]
        [InlineData("method")]
        [InlineData("path")]
        [InlineData("url")]
        [InlineData("headers")]
        [InlineData("Authorization")]
        [InlineData("accessToken")]
        [InlineData("caller")]
        [InlineData("methodOverride")]
        public void ValidateJson_ArbitraryRequestControls_AreRejectedAtEveryObjectLevel(string member)
        {
            AssertFailure(Policy().ValidateJson("{\"operation\":\"systems.list\",\"parameters\":{},\"" + member + "\":\"secret\"}"), "invalid_json");
            AssertFailure(Policy().ValidateJson("{\"operation\":\"systems.list\",\"parameters\":{\"" + member + "\":\"secret\"}}"), "invalid_json");
        }

        [Theory]
        [InlineData("page", "-1")]
        [InlineData("page", "1000")]
        [InlineData("page", "2147483648")]
        [InlineData("page", "\"0\"")]
        [InlineData("page", "0.0")]
        [InlineData("page", "1e0")]
        [InlineData("page", "null")]
        [InlineData("page", "true")]
        [InlineData("page", "{}")]
        [InlineData("page", "[]")]
        [InlineData("pageSize", "0")]
        [InlineData("pageSize", "251")]
        [InlineData("pageSize", "-1")]
        [InlineData("pageSize", "\"100\"")]
        [InlineData("pageSize", "100.0")]
        [InlineData("pageSize", "null")]
        [InlineData("nameContains", "null")]
        [InlineData("nameContains", "7")]
        [InlineData("nameContains", "true")]
        [InlineData("nameContains", "{}")]
        [InlineData("nameContains", "[]")]
        public void ValidateJson_InvalidScalarParameter_ReturnsSafeBadInput(string member, string value)
        {
            AssertFailure(Policy().ValidateJson("{\"operation\":\"systems.list\",\"parameters\":{\"" + member + "\":" + value + "}}"),
                "invalid_parameter", "parameters." + member);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(999, 250)]
        public void ValidateJson_LimitValues_ProducesExactPagination(int page, int pageSize)
        {
            var result = Policy().ValidateJson(JsonSerializer.Serialize(new { operation = "systems.list", parameters = new { page, pageSize } }));
            Assert.True(result.Ok);
            Assert.Equal($"?page={page}&pageSize={pageSize}", result.Value.RequestUri.Query);
        }

        [Fact]
        public void ValidateJson_NameText_IsEncodedAsOneScalarOnFixedRoute()
        {
            const string name = "æøå 💻 &page=999#fragment?url=https://attacker.example/%2f\r\nX-HTTP-Method-Override:POST";
            var result = Policy().ValidateJson(RequestWithName(name));
            Assert.True(result.Ok);
            Assert.Equal("GET", result.Value.Method);
            Assert.Equal("kitos.example", result.Value.RequestUri.Host);
            Assert.Equal("/api/v2/it-systems", result.Value.RequestUri.AbsolutePath);
            Assert.Equal(string.Empty, result.Value.RequestUri.Fragment);
            var query = result.Value.RequestUri.Query.TrimStart('?').Split('&');
            Assert.Equal(3, query.Length);
            Assert.Equal("page=0", query[0]);
            Assert.Equal("pageSize=100", query[1]);
            Assert.Equal(name, Uri.UnescapeDataString(query[2]["nameContains=".Length..]));
            Assert.Contains("%26page%3D999", query[2]);
            Assert.Contains("%252f", query[2]);
        }

        [Fact]
        public void ValidateJson_NameLengthLimits_AllowEmptyAndExactBound()
        {
            Assert.True(Policy().ValidateJson(RequestWithName(string.Empty)).Ok);
            Assert.True(Policy().ValidateJson(RequestWithName(new string('a', ReadRequestPolicy.MaxNameLength))).Ok);
            AssertFailure(Policy().ValidateJson(RequestWithName(new string('a', ReadRequestPolicy.MaxNameLength + 1))), "invalid_parameter", "parameters.nameContains");
        }

        [Fact]
        public void ValidateJson_ByteLimit_UsesUtf8SizeAndAcceptsExactBoundary()
        {
            const string json = "{\"operation\":\"systems.list\",\"parameters\":{}}";
            var exact = json + new string(' ', ReadRequestPolicy.MaxJsonBytes - Encoding.UTF8.GetByteCount(json));
            Assert.True(Policy().ValidateJson(exact).Ok);
            AssertFailure(Policy().ValidateJson(exact + " "), "request_size", "$");
            var unicode = "{\"operation\":\"" + new string('æ', ReadRequestPolicy.MaxJsonBytes / 2) + "\",\"parameters\":{}}";
            Assert.True(unicode.Length < ReadRequestPolicy.MaxJsonBytes);
            AssertFailure(Policy().ValidateJson(unicode), "request_size", "$");
            AssertFailure(Policy().ValidateJson(null), "invalid_json", "$");
        }

        [Fact]
        public void ValidateJson_ExcessiveNesting_ReturnsSafeBadInput()
        {
            AssertFailure(Policy().ValidateJson("{\"operation\":\"systems.list\",\"parameters\":{\"nameContains\":[[[[]]]]}}"), "invalid_json");
        }

        [Theory]
        [InlineData("http://kitos.example")]
        [InlineData("https://kitos.example/api")]
        [InlineData("https://kitos.example/api/../")]
        [InlineData("https://kitos.example/api/%2e%2e/")]
        [InlineData("https://kitos.example?option=true")]
        [InlineData("https://kitos.example?")]
        [InlineData("https://kitos.example#fragment")]
        [InlineData("https://kitos.example#")]
        [InlineData("https://user:password@kitos.example")]
        [InlineData("https://@kitos.example")]
        [InlineData("/relative")]
        public void Constructor_InvalidConfiguredOrigin_FailsClearly(string origin)
        {
            Assert.ThrowsAny<ArgumentException>(() => new ReadRequestPolicy(new Uri(origin, UriKind.RelativeOrAbsolute)));
        }

        [Theory]
        [InlineData("https://kitos.example", "https://kitos.example/api/v2/it-systems")]
        [InlineData("https://kitos.example/", "https://kitos.example/api/v2/it-systems")]
        [InlineData("https://kitos.example:8443/", "https://kitos.example:8443/api/v2/it-systems")]
        [InlineData("https://[::1]:8443/", "https://[::1]:8443/api/v2/it-systems")]
        public void Constructor_ValidConfiguredOrigin_PreservesApprovedDestination(string origin, string destination)
        {
            var result = new ReadRequestPolicy(new Uri(origin)).ValidateJson("{\"operation\":\"systems.list\",\"parameters\":{}}");
            Assert.True(result.Ok);
            Assert.StartsWith(destination + "?", result.Value.RequestUri.AbsoluteUri);
        }

        [Fact]
        public void ValidateJson_Result_HasNoPublicConstructorOrWritableProperties()
        {
            Assert.Empty(typeof(ValidatedReadRequest).GetConstructors());
            Assert.All(typeof(ValidatedReadRequest).GetProperties(), property => Assert.Null(property.SetMethod));
        }

        [Theory]
        [InlineData("{\"operation\":\"\\uD800\",\"parameters\":{}}")]
        [InlineData("{\"operation\":\"systems.list\",\"parameters\":{\"nameContains\":\"\\uD800\"}}")]
        [InlineData("{\"operation\":\"systems.list\",\"parameters\":{\"\\uD800\":\"anything\"}}")]
        public void ValidateJson_InvalidEscapedUnicode_ReturnsSafeFailure(string json)
        {
            Assert.True(Policy().ValidateJson(json).Failed);
        }

        [Fact]
        public void ValidateJson_InvalidUnescapedUnicode_ReturnsSafeFailure()
        {
            var json = "{\"operation\":\"systems.list\",\"parameters\":{\"nameContains\":\"" + '\uD800' + "\"}}";
            AssertFailure(Policy().ValidateJson(json), "invalid_json");
        }

        [Fact]
        public void ValidateJson_NonEnglishCurrentCulture_UsesInvariantQueryNumbers()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-EG");
                var result = Policy().ValidateJson("{\"operation\":\"systems.list\",\"parameters\":{\"page\":999,\"pageSize\":250}}");
                Assert.True(result.Ok);
                Assert.Equal("?page=999&pageSize=250", result.Value.RequestUri.Query);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        private static ReadRequestPolicy Policy() => new(new Uri("https://kitos.example"));
        private static string RequestWithName(string nameContains) =>
            JsonSerializer.Serialize(new { operation = "systems.list", parameters = new { nameContains } });
        private static void AssertFailure(Result<ValidatedReadRequest, OperationError> result, string code, string? path = null)
        {
            Assert.True(result.Failed);
            var error = Assert.IsType<ReadRequestValidationError>(result.Error);
            Assert.Equal(OperationFailure.BadInput, error.FailureType);
            Assert.Equal(code, error.Code);
            if (path is not null) Assert.Equal(path, error.Path);
            Assert.DoesNotContain("secret", error.ToString());
            Assert.DoesNotContain("attacker", error.ToString());
        }
    }
}
