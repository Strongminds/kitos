using Core.Abstractions.Types;
using Mcp.Worker;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Tests.Unit.Mcp.Worker
{
    public class WorkerBehaviorTest
    {
        internal const string Plan = """{"catalogVersion":"1.0.0","entity":"systems","projection":["uuid","name"],"filters":[{"field":"name","operator":"contains","value":"Example"}],"traversal":[]}""";
        internal const string Envelope = """{"data":[{"uuid":"11111111-1111-1111-1111-111111111111","name":"Example"}],"coverage":{"scope":"caller_visible","execution":"single_page","consistency":"best_effort","startedAt":"2026-10-13T10:00:00Z","completedAt":"2026-10-13T10:00:01Z","page":0,"pageSize":100,"mayHaveMore":false},"catalogVersion":"1.0.0"}""";
        internal static readonly string Grant = new('a', 64);

        [Fact]
        public void Prepare_ExecutableShape_PreservesTermAndFixedPaging()
        {
            var result = new KitosQueryService().Prepare(Plan);
            Assert.True(result.Ok);
            Assert.Equal("systems.list", result.Value.Operation);
            Assert.Equal(new SystemListParameters("Example", 0, 100), result.Value.Parameters);
            var discovery = KitosQueryService.Discover();
            Assert.Equal("systems_name_query_single_page", discovery.GetProperty("capability").GetString());
            Assert.Contains("one page", discovery.GetProperty("limitations").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("null")]
        [InlineData("{")]
        public void Prepare_MalformedPlan_ReturnsExpectedFailure(string plan) => Assert.True(new KitosQueryService().Prepare(plan).Failed);

        [Fact]
        public void Prepare_ValidButUnsupportedExecution_ReturnsFailure()
        {
            Assert.True(new KitosQueryService().Prepare(Plan.Replace("\"contains\"", "\"eq\"", StringComparison.Ordinal)).Failed);
            Assert.True(new KitosQueryService().Prepare(Plan.Replace("[\"uuid\",\"name\"]", "[\"name\"]", StringComparison.Ordinal)).Failed);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t\r\n")]
        public void Prepare_WhitespaceOnlyFilter_DoesNotPrepareARead(string term)
        {
            var plan = Plan.Replace("\"Example\"", System.Text.Json.JsonSerializer.Serialize(term), StringComparison.Ordinal);
            Assert.True(new KitosQueryService().Prepare(plan).Failed);
        }

        [Fact]
        public async Task ReadAsync_StreamFailure_ReturnsSanitizedIncompleteOutcome()
        {
            using var handler = new StreamFailureHandler();
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://broker.example") };
            var result = await new BoundaryClient(http).ReadAsync(new("systems.list", new("Example")), Grant, CancellationToken.None);
            Assert.True(result.Failed);
            Assert.Equal("read_incomplete", result.Error.Message);
            Assert.DoesNotContain("secret", result.Error.ToString());
        }

        [Fact]
        public async Task ReadAsync_ValidGrant_ComposesBrokerPostAndPreservesSafeResult()
        {
            using var handler = new BrokerStub(HttpStatusCode.OK, Envelope);
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://broker.example") };
            var result = await new BoundaryClient(http).ReadAsync(new("systems.list", new("Example")), Grant, CancellationToken.None);
            Assert.True(result.Ok);
            Assert.Equal(1, handler.Calls);
            Assert.Equal(HttpMethod.Post, handler.Method);
            Assert.Equal("https://broker.example/reads", handler.Uri!.AbsoluteUri);
            Assert.Equal(Grant, handler.Grant);
            Assert.Equal("Example", result.Value.GetProperty("data")[0].GetProperty("name").GetString());
        }

        [Fact]
        public async Task ReadAsync_InvalidGrant_PerformsNoDependencyCall()
        {
            using var handler = new BrokerStub(HttpStatusCode.OK, Envelope);
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://broker.example") };
            var result = await new BoundaryClient(http).ReadAsync(new("systems.list", new("Example")), "KITOS-JWT.invalid", CancellationToken.None);
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
            Assert.Equal(0, handler.Calls);
        }

        [Theory]
        [InlineData(401, "authorization_required")]
        [InlineData(403, "authorization_required")]
        [InlineData(429, "read_budget_exceeded")]
        [InlineData(500, "read_incomplete")]
        [InlineData(302, "read_incomplete")]
        public async Task ReadAsync_DeniedDependency_ReturnsSanitizedFailure(int status, string error)
        {
            using var handler = new BrokerStub((HttpStatusCode)status, "secret dependency body");
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://broker.example") };
            var result = await new BoundaryClient(http).ReadAsync(new("systems.list", new("Example")), Grant, CancellationToken.None);
            Assert.True(result.Failed);
            Assert.Equal(error, result.Error.Message);
            Assert.DoesNotContain("secret", result.Error.ToString());
        }

        [Theory]
        [InlineData("{")]
        [InlineData("[]")]
        [InlineData("{\"data\":[],\"coverage\":{},\"catalogVersion\":\"0.9.0\"}")]
        [InlineData("{\"data\":[{\"token\":\"secret\"}],\"coverage\":{},\"catalogVersion\":\"1.0.0\"}")]
        public async Task ReadAsync_MalformedOrIncompatibleResponse_ReturnsFailure(string body)
        {
            using var handler = new BrokerStub(HttpStatusCode.OK, body);
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://broker.example") };
            var result = await new BoundaryClient(http).ReadAsync(new("systems.list", new("Example")), Grant, CancellationToken.None);
            Assert.True(result.Failed);
            Assert.DoesNotContain("secret", result.Error.ToString());
        }

        [Fact]
        public async Task ReadAsync_ExcessiveBody_ReturnsBoundedFailure()
        {
            using var handler = new BrokerStub(HttpStatusCode.OK, new string(' ', 2 * 1024 * 1024 + 1));
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://broker.example") };
            Assert.True((await new BoundaryClient(http).ReadAsync(new("systems.list", new("Example")), Grant, CancellationToken.None)).Failed);
        }

        [Fact]
        public async Task ReadAsync_UnexpectedSensitiveEnvelopeFields_AreNotReturned()
        {
            foreach (var body in new[]
            {
                Envelope.Replace("\"catalogVersion\":\"1.0.0\"", "\"catalogVersion\":\"1.0.0\",\"token\":\"secret\"", StringComparison.Ordinal),
                Envelope.Replace("\"mayHaveMore\":false", "\"mayHaveMore\":false,\"token\":\"secret\"", StringComparison.Ordinal),
                Envelope.Replace("\"name\":\"Example\"", "\"name\":\"Example\",\"token\":\"secret\"", StringComparison.Ordinal)
            })
            {
                using var handler = new BrokerStub(HttpStatusCode.OK, body);
                using var http = new HttpClient(handler) { BaseAddress = new Uri("https://broker.example") };
                var result = await new BoundaryClient(http).ReadAsync(new("systems.list", new("Example")), Grant, CancellationToken.None);
                Assert.True(result.Failed);
                Assert.DoesNotContain("secret", result.Error.ToString());
            }
        }

        [Fact]
        public void PeerCertificate_PinAndIdentityFailures_AreRejected()
        {
            using var cert = Certificate("localhost");
            var pin = cert.GetCertHashString(HashAlgorithmName.SHA256);
            Assert.True(PeerCertificate.Matches(cert, pin));
            Assert.False(PeerCertificate.Matches(null, pin));
            Assert.False(PeerCertificate.Matches(cert, new string('0', 64)));
            Assert.False(PeerCertificate.Matches(cert, "invalid"));
            Assert.False(PeerCertificate.MatchesServer(cert, pin, SslPolicyErrors.RemoteCertificateNameMismatch));
            Assert.False(PeerCertificate.MatchesServer(cert, pin, SslPolicyErrors.RemoteCertificateNotAvailable));
            Assert.True(PeerCertificate.MatchesServer(cert, pin, SslPolicyErrors.RemoteCertificateChainErrors));
        }

        internal static X509Certificate2 Certificate(string name)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder();
            names.AddDnsName(name);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            return X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
        }

        internal sealed class BrokerStub(HttpStatusCode status, string body) : HttpMessageHandler
        {
            internal int Calls { get; private set; }
            internal HttpMethod? Method { get; private set; }
            internal Uri? Uri { get; private set; }
            internal string? Grant { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                Method = request.Method;
                Uri = request.RequestUri;
                Grant = request.Headers.GetValues("X-Mcp-Read-Grant").Single();
                return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
            }
        }

        private sealed class StreamFailureHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new BrokenStream()) });
        }

        private sealed class BrokenStream : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => throw new NotSupportedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new IOException("secret interrupted broker response");
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.FromException<int>(new IOException("secret interrupted broker response"));
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
