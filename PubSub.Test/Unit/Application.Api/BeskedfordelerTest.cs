using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PubSub.Application.Api.Controllers;
using PubSub.Application.Api.UserSync;
using PubSub.Application.Api.UserSync.Beskedfordeler;
using PubSub.Core.DomainModel.UserSync;
using PubSub.Infrastructure.DataAccess;

namespace PubSub.Test.Unit.Application.Api;

public class BeskedfordelerTest
{
    [Theory]
    [InlineData(BeskedfordelerStatus.Accepted, 20)]
    [InlineData(BeskedfordelerStatus.CannotReceive, 40)]
    [InlineData(BeskedfordelerStatus.NotImplemented, 51)]
    [InlineData(BeskedfordelerStatus.Unavailable, 53)]
    [InlineData(BeskedfordelerStatus.UnsupportedVersion, 55)]
    public void Serialize_Status_UsesDocumentedResponseNamespaces(BeskedfordelerStatus status, int code)
    {
        var response = XElement.Parse(BeskedfordelerResponse.Serialize(status));
        Assert.Equal(BeskedfordelerResponse.Sts + "ModtagBeskedOutput", response.Name);
        var standardReturn = Assert.Single(response.Elements(BeskedfordelerResponse.Sagdok + "StandardRetur"));
        Assert.Equal(code.ToString(), standardReturn.Element(BeskedfordelerResponse.Sagdok + "StatusKode")?.Value);
        Assert.NotNull(standardReturn.Element(BeskedfordelerResponse.Sagdok + "FejlbeskedTekst"));
    }

    [Fact]
    public async Task Receive_ReplayAndConflict_PersistsOneDeliveryBeforeSuccess()
    {
        await using var db = CreateDb();
        var mapper = new Mock<IBeskedfordelerDeletionMapper>();
        var deletion = new UserDeletionEvent("ignored", Guid.NewGuid(), Guid.NewGuid());
        mapper.Setup(x => x.Map(It.IsAny<XElement>())).Returns(() => deletion);
        var receiver = new BeskedfordelerReceiver(mapper.Object, new UserChangeOutbox(db));
        var id = Guid.NewGuid();
        Assert.Equal(BeskedfordelerStatus.Accepted, await receiver.Receive(Input(id), default));
        Assert.Equal(id.ToString("D"), (await db.UserChangeDeliveries.SingleAsync()).ExternalMessageId);
        Assert.Equal(BeskedfordelerStatus.Accepted, await receiver.Receive(Input(id), default));
        deletion = deletion with { ExternalUserUuid = Guid.NewGuid() };
        Assert.Equal(BeskedfordelerStatus.CannotReceive, await receiver.Receive(Input(id), default));
        Assert.Equal(1, await db.UserChangeDeliveries.CountAsync());
    }

    [Fact]
    public async Task Receive_UnconfirmedContract_DoesNotPersistOrAcknowledge()
    {
        await using var db = CreateDb();
        var receiver = new BeskedfordelerReceiver(new UnsupportedBeskedfordelerDeletionMapper(), new UserChangeOutbox(db));
        Assert.Equal(BeskedfordelerStatus.NotImplemented, await receiver.Receive(Input(Guid.NewGuid()), default));
        Assert.Empty(db.UserChangeDeliveries);
    }

    [Theory]
    [InlineData("invalid", "1.0", BeskedfordelerStatus.CannotReceive)]
    [InlineData("00000000-0000-0000-0000-000000000000", "1.0", BeskedfordelerStatus.CannotReceive)]
    [InlineData("60000000-0000-0000-0000-000000000001", "9.0", BeskedfordelerStatus.UnsupportedVersion)]
    public async Task Receive_InvalidEnvelope_DoesNotMap(string id, string version, BeskedfordelerStatus status)
    {
        await using var db = CreateDb();
        var mapper = new Mock<IBeskedfordelerDeletionMapper>(MockBehavior.Strict);
        var input = Input(Guid.NewGuid());
        input.Descendants(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator").Single().Value = id;
        input.Descendants(BeskedfordelerResponse.Envelope + "BeskedVersion").Single().Value = version;
        Assert.Equal(status, await new BeskedfordelerReceiver(mapper.Object, new UserChangeOutbox(db)).Receive(input, default));
        Assert.Empty(db.UserChangeDeliveries);
    }

    [Fact]
    public async Task Receive_PersistenceFailure_NeverReturnsSuccess()
    {
        await using var db = new FailingDb(new DbContextOptionsBuilder<PubSubContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var mapper = new Mock<IBeskedfordelerDeletionMapper>();
        mapper.Setup(x => x.Map(It.IsAny<XElement>())).Returns(new UserDeletionEvent("ignored", Guid.NewGuid(), Guid.NewGuid()));
        var receiver = new BeskedfordelerReceiver(mapper.Object, new UserChangeOutbox(db));
        await Assert.ThrowsAsync<DbUpdateException>(() => receiver.Receive(Input(Guid.NewGuid()), default));
        Assert.Empty(db.UserChangeDeliveries);
    }

    [Theory]
    [InlineData(false, false, 404)]
    [InlineData(true, false, 403)]
    [InlineData(true, true, 403)]
    public async Task Callback_DisabledOrUnauthenticated_CannotAccessReceiver(bool enabled, bool https, int expected)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Beskedfordeler:Enabled"] = enabled.ToString()
        }).Build();
        using var services = new ServiceCollection().BuildServiceProvider();
        var controller = new BeskedfordelerController(config, new BeskedfordelerClientCertificateValidator(config),
            services, NullLogger<BeskedfordelerController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Scheme = https ? "https" : "http";
        controller.Request.Headers.Authorization = "Bearer does-not-authorize-callback";
        var response = Assert.IsAssignableFrom<StatusCodeResult>(await controller.Receive(default));
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public void IsValid_PinnedButUntrustedCertificate_Rejects()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=callback-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Beskedfordeler:ClientCertificateSha256:0"] = certificate.GetCertHashString(HashAlgorithmName.SHA256)
        }).Build();
        var validator = new BeskedfordelerClientCertificateValidator(config);
        Assert.False(validator.IsValid(null));
        Assert.False(validator.IsValid(certificate));
    }

    [Fact]
    public async Task Read_ValidSchemaConformingDocument_Accepts()
    {
        using var stream = XmlBody("<ModtagBeskedInput xmlns='urn:oio:sts:1.0.0'>7</ModtagBeskedInput>");
        Assert.Equal("7", (await Reader().Read(stream, default)).Value);
    }

    [Theory]
    [InlineData("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///does-not-exist'>]><ModtagBeskedInput xmlns='urn:oio:sts:1.0.0'>&e;</ModtagBeskedInput>")]
    [InlineData("<ModtagBeskedInput xmlns='urn:oio:sts:1.0.0'>not-an-integer</ModtagBeskedInput>")]
    [InlineData("<ModtagBeskedInput xmlns='wrong'>7</ModtagBeskedInput>")]
    [InlineData("<ModtagBeskedInput")]
    public async Task Read_InvalidOrUnsafeXml_Rejects(string xml)
    {
        using var stream = XmlBody(xml);
        var error = await Record.ExceptionAsync(() => Reader().Read(stream, default));
        Assert.True(error is XmlException or XmlSchemaException);
    }

    [Fact]
    public async Task Read_OversizedDocument_Rejects()
    {
        using var stream = XmlBody("<ModtagBeskedInput xmlns='urn:oio:sts:1.0.0'>" + new string('1', BeskedfordelerXmlReader.MaxBodySize) + "</ModtagBeskedInput>");
        await Assert.ThrowsAsync<XmlException>(() => Reader().Read(stream, default));
    }

    [Fact]
    public async Task Callback_DurableCommitPending_DoesNotAcknowledgeEarly()
    {
        await using var db = new DelayedDb(new DbContextOptionsBuilder<PubSubContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var mapper = new Mock<IBeskedfordelerDeletionMapper>();
        mapper.Setup(x => x.Map(It.IsAny<XElement>())).Returns(new UserDeletionEvent("ignored", Guid.NewGuid(), Guid.NewGuid()));
        var receiver = new BeskedfordelerReceiver(mapper.Object, new UserChangeOutbox(db));
        var pending = receiver.Receive(Input(Guid.NewGuid()), default);
        await db.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(pending.IsCompleted);
        Assert.Empty(db.UserChangeDeliveries);
        db.AllowCommit.SetResult();
        Assert.Equal(BeskedfordelerStatus.Accepted, await pending.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Single(db.UserChangeDeliveries);
    }

    [Theory]
    [InlineData(false, "application/xml", 51)]
    [InlineData(false, "application/json", 40)]
    [InlineData(true, "application/xml", 53)]
    public async Task Callback_AuthenticatedRequest_ReturnsXmlFailureWithoutAcknowledging(bool persistenceFailure, string contentType, int expected)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Beskedfordeler:Enabled"] = "true",
            ["UserSync:Enabled"] = "true"
        }).Build();
        var certificates = new Mock<IBeskedfordelerClientCertificateValidator>();
        certificates.Setup(x => x.IsValid(It.IsAny<X509Certificate2?>())).Returns(true);
        await using var db = persistenceFailure
            ? new FailingDb(new DbContextOptionsBuilder<PubSubContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
            : CreateDb();
        var mapper = new Mock<IBeskedfordelerDeletionMapper>();
        mapper.Setup(x => x.Map(It.IsAny<XElement>())).Returns(persistenceFailure
            ? new UserDeletionEvent("ignored", Guid.NewGuid(), Guid.NewGuid()) : null);
        // The test schema validates only the wrapper; official-schema acceptance needs the missing dependencies.
        var schemas = new XmlSchemaSet { XmlResolver = null };
        using var schemaText = new StringReader("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:oio:sts:1.0.0'><xs:element name='ModtagBeskedInput'><xs:complexType><xs:sequence><xs:any processContents='skip'/></xs:sequence></xs:complexType></xs:element></xs:schema>");
        using var schemaReader = XmlReader.Create(schemaText);
        schemas.Add(null, schemaReader);
        schemas.Compile();
        using var services = new ServiceCollection()
            .AddSingleton(new BeskedfordelerXmlReader(schemas))
            .AddSingleton(new BeskedfordelerReceiver(mapper.Object, new UserChangeOutbox(db)))
            .BuildServiceProvider();
        var controller = new BeskedfordelerController(config, certificates.Object, services, NullLogger<BeskedfordelerController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Scheme = "https";
        controller.Request.ContentType = contentType;
        using var body = XmlBody(Input(Guid.NewGuid()).ToString());
        controller.Request.Body = body;
        var result = Assert.IsType<ContentResult>(await controller.Receive(default));
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("application/xml; charset=utf-8", result.ContentType);
        Assert.Equal(expected.ToString(), XElement.Parse(result.Content!).Descendants(BeskedfordelerResponse.Sagdok + "StatusKode").Single().Value);
        Assert.Empty(db.UserChangeDeliveries);
    }

    [Fact]
    public void LoadSchemas_IncompleteSet_RejectsAtStartup()
    {
        var directory = Directory.CreateTempSubdirectory("kitos-beskedfordeler-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "callback.xsd"),
                "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:sd='urn:missing' targetNamespace='urn:oio:sts:1.0.0'><xs:import namespace='urn:missing' schemaLocation='missing.xsd'/><xs:element name='ModtagBeskedInput' type='sd:MissingType'/></xs:schema>");
            Assert.ThrowsAny<XmlSchemaException>(() => BeskedfordelerXmlReader.LoadSchemas(directory.FullName));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task LoadSchemas_LocalImports_CompilesWithoutExternalResolution()
    {
        var directory = Directory.CreateTempSubdirectory("kitos-beskedfordeler-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "callback.xsd"),
                "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:sd='urn:local' targetNamespace='urn:oio:sts:1.0.0'><xs:import namespace='urn:local' schemaLocation='https://invalid.example/never-fetch.xsd'/><xs:element name='ModtagBeskedInput' type='sd:InputType'/></xs:schema>");
            File.WriteAllText(Path.Combine(directory.FullName, "dependency.xsd"),
                "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:local'><xs:simpleType name='InputType'><xs:restriction base='xs:int'/></xs:simpleType></xs:schema>");
            var reader = new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas(directory.FullName));
            using var body = XmlBody("<ModtagBeskedInput xmlns='urn:oio:sts:1.0.0'>7</ModtagBeskedInput>");
            Assert.Equal("7", (await reader.Read(body, default)).Value);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    // A deliberately small schema tests parser behavior, not the unavailable producer contract.
    private static BeskedfordelerXmlReader Reader()
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };
        using var text = new StringReader("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:oio:sts:1.0.0'><xs:element name='ModtagBeskedInput' type='xs:int'/></xs:schema>");
        using var reader = XmlReader.Create(text);
        schemas.Add(null, reader);
        schemas.Compile();
        return new BeskedfordelerXmlReader(schemas);
    }

    private static MemoryStream XmlBody(string xml) => new(Encoding.UTF8.GetBytes(xml));

    private static XElement Input(Guid id) => new(BeskedfordelerResponse.Sts + "ModtagBeskedInput",
        new XElement(BeskedfordelerResponse.Envelope + "Haendelsesbesked",
            new XElement(BeskedfordelerResponse.Envelope + "BeskedId",
                new XElement(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator", id)),
            new XElement(BeskedfordelerResponse.Envelope + "BeskedVersion", "1.0")));

    private static PubSubContext CreateDb() => new(new DbContextOptionsBuilder<PubSubContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FailingDb(DbContextOptions<PubSubContext> options) : PubSubContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Simulated persistence failure.");
    }

    private sealed class DelayedDb(DbContextOptions<PubSubContext> options) : PubSubContext(options)
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowCommit { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Started.SetResult();
            await AllowCommit.Task.WaitAsync(cancellationToken);
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
