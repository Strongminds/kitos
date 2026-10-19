using Mcp.Broker;
using Mcp.ReadBoundary.Core;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tests.Unit.Mcp.Broker
{
    public class KitosClientTest
    {
        private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> action) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(action(request));
        }
        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        [Fact]
        public async Task Authenticate_ValidIntrospection_UsesFixedValidationPostAndCurrentAuthorizedGet()
        {
            var count = 0;
            using var client = new HttpClient(new Handler(request =>
            {
                count++;
                Assert.Equal("kitos.example", request.RequestUri?.Host);
                if (count == 1)
                {
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Equal("/api/v2/token/validate", request.RequestUri?.AbsolutePath);
                    Assert.Null(request.Headers.Authorization);
                    return Json(JsonSerializer.Serialize(new { active = true, expiration = DateTimeOffset.UtcNow.AddMinutes(5), claims = new[] { new { type = ClaimTypes.Name, value = "42" } } }));
                }
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v2/it-systems", request.RequestUri?.AbsolutePath);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("secret", request.Headers.Authorization?.Parameter);
                return Json("[]");
            }));
            var result = await new KitosClient(client, new Uri("https://kitos.example"), TimeProvider.System).Authenticate("secret", CancellationToken.None);
            Assert.Equal("42", result?.User);
            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("null")]
        [InlineData("{}")]
        [InlineData("{\"active\":false}")]
        public async Task Authenticate_InvalidVerifiedShape_DeniesWithoutDataProbe(string json)
        {
            var count = 0;
            using var client = new HttpClient(new Handler(_ => { count++; return Json(json); }));
            Assert.Null(await new KitosClient(client, new Uri("https://kitos.example"), TimeProvider.System).Authenticate("secret", CancellationToken.None));
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Read_ValidApiResponse_ProjectsOnlyUuidNameAndHonestCoverage()
        {
            var identity = Guid.NewGuid();
            using var client = new HttpClient(new Handler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("secret", request.Headers.Authorization?.Parameter);
                return Json(JsonSerializer.Serialize(new[] { new { uuid = identity, name = "Known system", hidden = "not returned" } }));
            }));
            var request = new ReadRequestPolicy(new Uri("https://kitos.example")).ValidateJson("{\"operation\":\"systems.list\",\"parameters\":{}}").Value;
            var credential = new Credential("id", "42", "secret", DateTimeOffset.UtcNow.AddMinutes(5), "version");
            var result = await new KitosClient(client, new Uri("https://kitos.example"), TimeProvider.System).Read(request, credential, CancellationToken.None);
            Assert.NotNull(result);
            var serialized = JsonSerializer.SerializeToElement(result);
            Assert.Equal(3, serialized.EnumerateObject().Count());
            var row = serialized.GetProperty("data")[0];
            Assert.Equal(2, row.EnumerateObject().Count());
            Assert.Equal(identity, row.GetProperty("uuid").GetGuid());
            var coverage = serialized.GetProperty("coverage");
            Assert.Equal(8, coverage.EnumerateObject().Count());
            Assert.Equal("single_page", coverage.GetProperty("execution").GetString());
            Assert.False(coverage.GetProperty("mayHaveMore").GetBoolean());
            Assert.DoesNotContain("secret", serialized.ToString());
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[{}]")]
        [InlineData("[{\"uuid\":\"invalid\",\"name\":\"Name\"}]")]
        [InlineData("[{\"uuid\":\"00000000-0000-0000-0000-000000000000\",\"name\":\"Name\"}]")]
        [InlineData("[{\"uuid\":\"aa337e79-ae57-48cf-ab15-411010069f71\",\"name\":\" \"}]")]
        public async Task Read_InvalidApiSchema_ReturnsFailureNotEmptySuccess(string json)
        {
            using var client = new HttpClient(new Handler(_ => Json(json)));
            var request = new ReadRequestPolicy(new Uri("https://kitos.example")).ValidateJson("{\"operation\":\"systems.list\",\"parameters\":{}}").Value;
            Assert.Null(await new KitosClient(client, new Uri("https://kitos.example"), TimeProvider.System).Read(request,
                new Credential("id", "42", "secret", DateTimeOffset.UtcNow.AddMinutes(5), "version"), CancellationToken.None));
        }
    }
}
