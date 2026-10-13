using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Users;
using Core.DomainModel.Organization;
using Core.DomainModel.SSO;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Repositories.SSO;
using Core.DomainServices.Users;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Presentation.Web.Infrastructure.Beskedfordeler;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure;

public class BeskedfordelerTest
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static BeskedfordelerXmlReader Reader() => new(BeskedfordelerXmlReader.LoadSchemas(
        Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Beskedfordeler", "Schemas")));

    [Theory]
    [InlineData(false, false, 403)]
    [InlineData(true, true, 53)]
    [InlineData(true, false, 20)]
    public async Task Receive_OnlyAcknowledgesAuthenticatedDurableEnqueue(bool authorized, bool fails, int status)
    {
        var certificates = new Mock<IBeskedfordelerClientCertificateValidator>();
        certificates.Setup(x => x.IsValid(null)).Returns(authorized);
        var client = new Mock<IBackgroundJobClient>();
        Job? captured = null;
        client.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns((Job job, IState _) => { captured = job; if (fails) throw new InvalidOperationException(); return "1"; });
        var services = new ServiceCollection().AddSingleton(certificates.Object).AddSingleton(client.Object).AddSingleton(Reader());
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Scheme = "https";
        context.Request.ContentType = "application/xml";
        var original = Encoding.UTF8.GetBytes(Input().ToString());
        context.Request.Body = new MemoryStream(original);
        var result = await BeskedfordelerEndpoint.Receive(context);
        if (!authorized)
        {
            Assert.Equal(403, ((IStatusCodeHttpResult)result).StatusCode);
            Assert.Null(captured);
        }
        else
        {
            Assert.Contains($">{status}</", ((Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult)result).ResponseContent);
            Assert.Equal(original, Convert.FromBase64String((string)captured!.Args[0]));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Process_ReplayPreservesOneEvent_ConflictsFailVisibly(bool conflict)
    {
        var organizations = new Mock<IOrganizationRepository>();
        organizations.Setup(x => x.GetByUuid(OrganizationId)).Returns(new Organization { Id = 1, Uuid = OrganizationId, FkOrgUsersConnected = true });
        var identities = new Mock<ISsoUserIdentityRepository>();
        identities.Setup(x => x.GetByExternalUuid(UserId)).Returns(Maybe<SsoUserIdentity>.None);
        var repository = new Mock<IExternalUserChangeRepository>();
        ExternalUserChange? saved = null;
        repository.Setup(x => x.FindByMessageId(MessageId.ToString("D"))).Returns(() => saved);
        repository.Setup(x => x.InsertAndSave(It.IsAny<ExternalUserChange>())).Returns((ExternalUserChange value) => saved = value);
        var job = new BeskedfordelerJob(Reader(), new OrgBrugerDeletionMapper(new Dictionary<string, Guid> { ["12345678"] = OrganizationId }),
            new ExternalUserChangeIngestionService(repository.Object, organizations.Object, identities.Object));
        var xml = Input();
        string Payload() => Convert.ToBase64String(Encoding.UTF8.GetBytes(xml.ToString()));
        await job.Process(Payload(), default);
        Assert.Equal(ExternalUserChangeStatus.Pending, saved!.Status);
        Assert.Null(saved.UserId);
        if (conflict)
        {
            xml.Descendants(BeskedfordelerResponse.Envelope + "ObjektId").Single().Elements().Single().Value = Guid.NewGuid().ToString();
            await Assert.ThrowsAsync<InvalidOperationException>(() => job.Process(Payload(), default));
        }
        else await job.Process(Payload(), default);
        repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Once);
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
    [InlineData(false)]
    [InlineData(true)]
    public void Certificate_UntrustedOrExpired_RejectsEvenAllowlistedFingerprint(bool expired)
    {
        using var key = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=callback-test", key,
            System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddDays(expired ? -1 : 1));
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Beskedfordeler:ClientCertificateSha256:0"] = certificate.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256) }).Build();
        Assert.False(new BeskedfordelerClientCertificateValidator(configuration).IsValid(certificate));
        Assert.False(new BeskedfordelerClientCertificateValidator(configuration).IsValid(null));
    }

    [Fact]
    public async Task Receive_OversizedRequest_DoesNotEnqueue()
    {
        var certificates = new Mock<IBeskedfordelerClientCertificateValidator>();
        certificates.Setup(x => x.IsValid(null)).Returns(true);
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        await using var provider = new ServiceCollection().AddSingleton(certificates.Object).AddSingleton(client.Object).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Scheme = "https";
        context.Request.ContentType = "application/xml";
        context.Request.Body = new MemoryStream(new byte[BeskedfordelerXmlReader.MaxBodySize + 1]);
        var result = await BeskedfordelerEndpoint.Receive(context);
        Assert.Contains(">40</", ((Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult)result).ResponseContent);
        client.VerifyNoOtherCalls();
    }
    [Theory]
    [InlineData(false, 404)]
    [InlineData(true, 403)]
    public async Task Route_HiddenAndIndependentOfUserMiddleware(bool enabled, int expectedStatus)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Beskedfordeler:Enabled"] = enabled.ToString() });
        builder.Services.AddSingleton(Mock.Of<IBeskedfordelerClientCertificateValidator>());
        await using var app = builder.Build();
        app.UseRouting();
        app.Use(async (context, next) =>
        {
            // A registered callback must execute its own authorization before ordinary user middleware.
            if (context.GetEndpoint() != null) throw new InvalidOperationException("User middleware reached callback.");
            await next(context);
        });
        app.MapBeskedfordeler();
        await app.StartAsync();
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints).ToArray();
        if (enabled)
            Assert.True(Assert.Single(endpoints).Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()!.ExcludeFromDescription);
        else Assert.Empty(endpoints);
        using var client = app.GetTestClient();
        using var response = await client.PostAsync("https://localhost/api/v2/integration/beskedfordeler", null);
        Assert.Equal(expectedStatus, (int)response.StatusCode);
    }
    // Synthetic, schema-valid fixture based on SF1500 ORG_Bruger 1.0 pp. 5-9; not captured production data.
    // The arbitrary audit payload and actor are deliberately unrelated to the affected user/municipality.
    private static XElement Input() => XElement.Parse($$"""
        <ModtagBeskedInput xmlns="urn:oio:sts:1.0.0" xmlns:b="urn:oio:besked:kuvert:1.0" xmlns:s="urn:oio:sagdok:3.0.0">
          <b:Haendelsesbesked>
            <b:BeskedId><s:UUIDIdentifikator>{{MessageId}}</s:UUIDIdentifikator></b:BeskedId>
            <b:BeskedVersion>1.0</b:BeskedVersion>
            <b:Beskedkuvert>
              <b:Filtreringsdata>
                <b:Beskedtype><s:UUIDIdentifikator>{{OrgBrugerDeletionMapper.MessageType}}</s:UUIDIdentifikator></b:Beskedtype>
                <b:BeskedAnsvarligAktoer><s:UUIDIdentifikator>dddddddd-dddd-dddd-dddd-dddddddddddd</s:UUIDIdentifikator></b:BeskedAnsvarligAktoer>
                <b:TilladtModtager><s:URNIdentifikator>urn:oio:cvr-nr:12345678</s:URNIdentifikator></b:TilladtModtager>
                <b:ObjektRegistrering>
                  <b:Registreringstidspunkt><s:TidsstempelDatoTid>2023-06-15T16:41:34.000+02:00</s:TidsstempelDatoTid></b:Registreringstidspunkt>
                  <b:ObjektAnsvarligMyndighed><s:URNIdentifikator>urn:oio:cvr-nr:12345678</s:URNIdentifikator></b:ObjektAnsvarligMyndighed>
                  <b:ObjektId><s:UUIDIdentifikator>{{UserId}}</s:UUIDIdentifikator></b:ObjektId>
                  <b:ObjektType><s:UUIDIdentifikator>{{OrgBrugerDeletionMapper.UserObjectType}}</s:UUIDIdentifikator></b:ObjektType>
                  <b:ObjektHandling><s:UUIDIdentifikator>{{OrgBrugerDeletionMapper.UserDeactivated}}</s:UUIDIdentifikator></b:ObjektHandling>
                </b:ObjektRegistrering>
              </b:Filtreringsdata>
              <b:Leveranceinformation />
            </b:Beskedkuvert>
            <b:Beskeddata><b:Base64 encoding="UTF-8">PGF1ZGl0IC8+</b:Base64></b:Beskeddata>
          </b:Haendelsesbesked>
        </ModtagBeskedInput>
        """);
}
