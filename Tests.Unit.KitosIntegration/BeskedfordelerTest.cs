using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;

using Moq;
using KitosIntegration.Infrastructure.Beskedfordeler;
using KitosIntegration.Services;
using Kitos.Integration.Contracts;
using KitosIntegration.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Unit.KitosIntegration;

public class BeskedfordelerTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Receive_RepositoryReturnsNoJobId_DoesNotAcknowledgeAcceptance(string? jobId)
    {
        var original = Encoding.UTF8.GetBytes(Input().ToString());
        var jobs = new Mock<IBeskedfordelerJobRepository>();
        jobs.Setup(repository => repository.Enqueue(It.Is<byte[]>(body => body.SequenceEqual(original))))
            .Returns(jobId);
        var receiver = new BeskedfordelerService(Reader(), jobs.Object, NullLogger<BeskedfordelerService>.Instance);
        using var body = new MemoryStream(original);

        var status = await receiver.Receive(body, CancellationToken.None);

        Assert.Equal(BeskedfordelerStatus.Unavailable, status);
        jobs.Verify(repository => repository.Enqueue(It.Is<byte[]>(bytes => bytes.SequenceEqual(original))), Times.Once);
    }

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static BeskedfordelerXmlReader Reader() => new(BeskedfordelerXmlReader.LoadSchemas());

    [Fact]
    public void LoadSchemas_WithoutDirectory_CompilesCompleteEmbeddedSchemaSet()
    {
        var schemas = BeskedfordelerXmlReader.LoadSchemas();

        Assert.True(schemas.IsCompiled);
        Assert.True(schemas.GlobalElements.Contains(new System.Xml.XmlQualifiedName(
            "ModtagBeskedInput", BeskedfordelerMessage.Sts.NamespaceName)));
        Assert.Equal(7, typeof(BeskedfordelerMessage).Assembly.GetManifestResourceNames()
            .Count(name => name.EndsWith(".xsd", StringComparison.Ordinal)));
    }

    [Theory]

    [InlineData(true, 53)]
    [InlineData(false, 20)]
    public async Task Receive_OnlyAcknowledgesDurableEnqueue(bool fails, int status)
    {

        var client = new Mock<IBackgroundJobClient>();
        Job? captured = null;
        client.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns((Job job, IState _) => { captured = job; if (fails) throw new InvalidOperationException(); return "1"; });
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.ContentType = "application/xml";
        var original = Encoding.UTF8.GetBytes(Input().ToString());
        context.Request.Body = new MemoryStream(original);
        var result = await Controller(context, client.Object).Receive();
        Assert.Contains($">{status}</", ((ContentResult)result).Content);
        Assert.Equal(original, Convert.FromBase64String((string)captured!.Args[0]));
        var restored = Hangfire.Storage.InvocationData.SerializeJob(captured).DeserializeJob();
        Assert.Equal(typeof(IBeskedfordelerJob), restored.Type);
        var filters = new JobFilterAttributeFilterProvider().GetFilters(restored).Select(filter => filter.Instance).ToArray();
        Assert.Equal("beskedfordeler", Assert.Single(filters.OfType<QueueAttribute>()).Queue);
        var retry = Assert.Single(filters.OfType<AutomaticRetryAttribute>());
        Assert.Equal(8, retry.Attempts);
        Assert.Equal(new[] { 2, 4, 8, 16, 32, 64, 128, 256 }, retry.DelaysInSeconds);
        Assert.Equal(AttemptsExceededAction.Fail, retry.OnAttemptsExceeded);
    }
    [Theory]
    [InlineData("7654895c-a3f3-4c77-8afd-4c77330f963c", "1.0", 20)]
    [InlineData("7654895C-A3F3-4C77-8AFD-4C77330F963C", "1.0", 20)]
    [InlineData("7654895c-a3f3-4c77-8afd-4c77330f963c", "2.0", 55)]
    public async Task Receive_TestMessage_LogsWithoutEnqueue(string messageType, string version, int expectedStatus)
    {
        var xml = Input();
        xml.Descendants(BeskedfordelerMessage.Envelope + "Beskedtype").Single().Elements().Single().Value = messageType;
        xml.Descendants(BeskedfordelerMessage.Envelope + "BeskedVersion").Single().Value = version;
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BeskedfordelerService>>();
        var service = new BeskedfordelerService(Reader(), new HangfireBeskedfordelerJobRepository(client.Object), logger.Object);
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(xml.ToString()));

        var status = await service.Receive(body, CancellationToken.None);

        Assert.Equal(expectedStatus, (int)status);
        client.VerifyNoOtherCalls();
        logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Test/TestbeskedType1") &&
                state.ToString()!.Contains(MessageId.ToString()) && !state.ToString()!.Contains("<")),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            expectedStatus == 20 ? Times.Once() : Times.Never());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Receive_EnqueueOutcome_LogsTraceAndStatusWithoutPayloadOrExceptionDetails(bool fails)
    {
        const string sensitiveDetail = "private-payload-or-credential";
        const string traceId = "callback-test-trace";
        var client = new Mock<IBackgroundJobClient>();
        client.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns(() => fails ? throw new InvalidOperationException(sensitiveDetail) : "job-123");
        var logger = new Mock<ILogger<BeskedfordelerService>>();
        var service = new BeskedfordelerService(Reader(), new HangfireBeskedfordelerJobRepository(client.Object), logger.Object);
        var xml = Input();
        xml.Descendants(BeskedfordelerMessage.Envelope + "Base64").Single().Value =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(sensitiveDetail));
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(xml.ToString()));

        var status = await service.Receive(body, CancellationToken.None, traceId);

        Assert.Equal(fails ? BeskedfordelerStatus.Unavailable : BeskedfordelerStatus.Accepted, status);
        var logs = logger.Invocations.Where(x => x.Method.Name == nameof(ILogger.Log)).ToArray();
        Assert.NotEmpty(logs);
        Assert.All(logs, entry =>
        {
            Assert.Equal(LogLevel.Warning, entry.Arguments[0]);
            Assert.Null(entry.Arguments[3]);
            var text = entry.Arguments[2].ToString()!;
            Assert.Contains(traceId, text);
            Assert.DoesNotContain(sensitiveDetail, text);
            Assert.DoesNotContain(Convert.ToBase64String(Encoding.UTF8.GetBytes(sensitiveDetail)), text);
            Assert.DoesNotContain("<", text);
        });
        var output = string.Join("\n", logs.Select(x => x.Arguments[2].ToString()));
        Assert.Contains(fails ? "InvalidOperationException" : "job-123", output);
        Assert.Contains(fails ? "StatusKode 53" : "StatusKode 20", output);
    }

    [Fact]
    public async Task Receive_UnsupportedContentType_LogsRejectionWithoutReadingBody()
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BeskedfordelerV2Controller>>();
        var context = new DefaultHttpContext { TraceIdentifier = "rejected-callback" };
        context.Request.ContentType = "private-content-type";
        var controller = new BeskedfordelerV2Controller(
            new BeskedfordelerService(Reader(), new HangfireBeskedfordelerJobRepository(client.Object), NullLogger<BeskedfordelerService>.Instance),
            logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var response = Assert.IsType<ContentResult>(await controller.Receive());

        Assert.Contains(">40</", response.Content);
        client.VerifyNoOtherCalls();
        var logs = logger.Invocations.Where(x => x.Method.Name == nameof(ILogger.Log)).ToArray();
        Assert.All(logs, entry =>
        {
            Assert.Equal(LogLevel.Warning, entry.Arguments[0]);
            Assert.Contains(context.TraceIdentifier, entry.Arguments[2].ToString());
            Assert.DoesNotContain("private-content-type", entry.Arguments[2].ToString());
        });
        Assert.Contains(logs, entry => entry.Arguments[2].ToString()!.Contains("callback rejected"));
    }

    [Theory]
    [InlineData("<!DOCTYPE a [<!ENTITY x SYSTEM 'file:///secret'>]><a>&x;</a>")]
    [InlineData("<invalid />")]
    public async Task Read_UnsafeOrUnknownXml_Rejects(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        await Assert.ThrowsAnyAsync<Exception>(() => Reader().Read(stream, default));
    }
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("  ,  ", 0)]
    [InlineData("ABC", 1)]
    [InlineData(" ABC , DEF ", 2)]
    public void Certificate_AppSetting_ParsesThumbprintList(string? value, int expectedCount)
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["AppSettings:BeskedfordelerCertificateThumbprint"] = value }).Build();
        var thumbprints = BeskedfordelerClientCertificateValidator.GetAllowedThumbprints(configuration);
        Assert.Equal(expectedCount, thumbprints.Length);
        if (expectedCount > 0) Assert.Equal("ABC", thumbprints[0]);
        if (expectedCount > 1) Assert.Equal("DEF", thumbprints[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Certificate_UntrustedOrExpired_RejectsEvenAllowlistedThumbprint(bool expired)
    {
        using var key = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=callback-test", key,
            System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddDays(expired ? -1 : 1));
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["AppSettings:BeskedfordelerCertificateThumbprint"] =
                    $" {new string('0', 40)}, {certificate.Thumbprint.ToLowerInvariant()} "
            }).Build();
        var logger = new Mock<ILogger<BeskedfordelerClientCertificateValidator>>();
        var validator = new BeskedfordelerClientCertificateValidator(configuration, logger.Object);
        Assert.False(validator.IsValid(certificate));
        Assert.False(validator.IsValid(null));
        logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("chain validation completed") &&
                state.ToString()!.Contains("Valid False") && state.ToString()!.Contains("UntrustedRoot")),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("no client certificate")),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Certificate_NotAllowlisted_RejectsBeforeChainValidation(bool sha256)
    {
        using var key = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=callback-test", key,
            System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["AppSettings:BeskedfordelerCertificateThumbprint"] = sha256
                    ? certificate.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256)
                    : new string('0', 40)
            }).Build();
        var logger = new Mock<ILogger<BeskedfordelerClientCertificateValidator>>();
        var validator = new BeskedfordelerClientCertificateValidator(configuration, logger.Object);

        Assert.False(validator.IsValid(certificate));
        logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("thumbprint not allowlisted") &&
                state.ToString()!.Contains(certificate.Thumbprint)),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Receive_OversizedRequest_DoesNotEnqueue()
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.ContentType = "application/xml";
        context.Request.Body = new MemoryStream(new byte[BeskedfordelerXmlReader.MaxBodySize + 1]);
        var result = await Controller(context, client.Object).Receive();
        Assert.Contains(">40</", ((ContentResult)result).Content);
        client.VerifyNoOtherCalls();
    }
    private static BeskedfordelerV2Controller Controller(HttpContext context,
        IBackgroundJobClient client) =>
        new(new BeskedfordelerService(Reader(), new HangfireBeskedfordelerJobRepository(client), NullLogger<BeskedfordelerService>.Instance),
            NullLogger<BeskedfordelerV2Controller>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    // Synthetic, schema-valid fixture based on SF1500 ORG_Bruger 1.0 pp. 5-9; not captured production data.
    // The arbitrary audit payload and actor are deliberately unrelated to the affected user/municipality.
    private static XElement Input() => XElement.Parse($$"""
        <ModtagBeskedInput xmlns="urn:oio:sts:1.0.0" xmlns:b="urn:oio:besked:kuvert:1.0" xmlns:s="urn:oio:sagdok:3.0.0">
          <b:Haendelsesbesked>
            <b:BeskedId><s:UUIDIdentifikator>{{MessageId}}</s:UUIDIdentifikator></b:BeskedId>
            <b:BeskedVersion>1.0</b:BeskedVersion>
            <b:Beskedkuvert>
              <b:Filtreringsdata>
                <b:Beskedtype><s:UUIDIdentifikator>94ef3c82-10e9-4d12-a9d9-7a7242d5ba83</s:UUIDIdentifikator></b:Beskedtype>
                <b:BeskedAnsvarligAktoer><s:UUIDIdentifikator>dddddddd-dddd-dddd-dddd-dddddddddddd</s:UUIDIdentifikator></b:BeskedAnsvarligAktoer>
                <b:TilladtModtager><s:URNIdentifikator>urn:oio:cvr-nr:12345678</s:URNIdentifikator></b:TilladtModtager>
                <b:ObjektRegistrering>
                  <b:Registreringstidspunkt><s:TidsstempelDatoTid>2023-06-15T16:41:34.000+02:00</s:TidsstempelDatoTid></b:Registreringstidspunkt>
                  <b:ObjektAnsvarligMyndighed><s:URNIdentifikator>urn:oio:cvr-nr:12345678</s:URNIdentifikator></b:ObjektAnsvarligMyndighed>
                  <b:ObjektId><s:UUIDIdentifikator>{{UserId}}</s:UUIDIdentifikator></b:ObjektId>
                  <b:ObjektType><s:UUIDIdentifikator>2b808769-8d09-4aa3-ac28-f8ac3526010f</s:UUIDIdentifikator></b:ObjektType>
                  <b:ObjektHandling><s:UUIDIdentifikator>9edb0c34-36e2-4d87-aba8-3ca7ade718ee</s:UUIDIdentifikator></b:ObjektHandling>
                </b:ObjektRegistrering>
              </b:Filtreringsdata>
              <b:Leveranceinformation />
            </b:Beskedkuvert>
            <b:Beskeddata><b:Base64 encoding="UTF-8">PGF1ZGl0IC8+</b:Base64></b:Beskeddata>
          </b:Haendelsesbesked>
        </ModtagBeskedInput>
        """);
}

