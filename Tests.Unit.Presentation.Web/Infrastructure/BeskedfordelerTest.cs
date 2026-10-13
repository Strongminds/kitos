using System;
using Core.DomainServices;
using Microsoft.Extensions.Configuration;
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

using Moq;
using Infrastructure.STS.Beskedfordeler;
using Presentation.Web.Controllers.API.V2.Integration;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure;

public class BeskedfordelerTest
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static BeskedfordelerXmlReader Reader() => new(BeskedfordelerXmlReader.LoadSchemas(
        Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Beskedfordeler", "Schemas")));

    [Fact]
    public void LegacyJob_CanDeserializeAndRetainsRetryAndQueuePolicies()
    {
        var legacyType = typeof(global::Presentation.Web.Infrastructure.Beskedfordeler.BeskedfordelerJob);
        var method = legacyType.GetMethod(nameof(BeskedfordelerJob.Process))!;
        var original = new Job(legacyType, method, "cGF5bG9hZA==", CancellationToken.None);
        var restored = Hangfire.Storage.InvocationData.SerializeJob(original).DeserializeJob();

        Assert.Equal(legacyType, restored.Type);
        Assert.Equal(typeof(BeskedfordelerJob), restored.Method.DeclaringType);
        Assert.Equal("cGF5bG9hZA==", restored.Args[0]);
        Assert.Equal(BeskedfordelerJob.QueueName,
            Assert.Single(restored.Method.GetCustomAttributes(typeof(QueueAttribute), true).Cast<QueueAttribute>()).Queue);
        Assert.Equal(8,
            Assert.Single(restored.Method.GetCustomAttributes(typeof(AutomaticRetryAttribute), true).Cast<AutomaticRetryAttribute>()).Attempts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Map_RequiresExactlyOneSignedUpOrganizationForSourceCvr(int matchingSignups)
    {
        var organizations = Enumerable.Range(0, matchingSignups)
            .Select(_ => new Organization { Uuid = Guid.NewGuid(), Cvr = "12345678", FkOrgUsersConnected = true })
            .Concat(new[]
            {
                new Organization { Uuid = Guid.NewGuid(), Cvr = "12345678", FkOrgUsersConnected = false },
                new Organization { Uuid = Guid.NewGuid(), Cvr = "87654321", FkOrgUsersConnected = true }
            }).ToArray();
        var repository = new Mock<IGenericRepository<Organization>>();
        repository.Setup(x => x.AsQueryable()).Returns(organizations.AsQueryable());
        var mapper = new OrgBrugerDeletionMapper(repository.Object);

        var result = mapper.Map(Input());

        if (matchingSignups == 1)
        {
            Assert.NotNull(result);
            Assert.Equal(organizations[0].Uuid, result.OrganizationUuid);
        }
        else Assert.Null(result);
    }

    [Fact]
    public void Map_ObservesSignupAndDisconnectWithoutRestart()
    {
        var organization = new Organization { Uuid = OrganizationId, Cvr = "12345678" };
        var repository = new Mock<IGenericRepository<Organization>>();
        repository.Setup(x => x.AsQueryable()).Returns(new[] { organization }.AsQueryable());
        var mapper = new OrgBrugerDeletionMapper(repository.Object);

        Assert.Null(mapper.Map(Input()));
        organization.FkOrgUsersConnected = true;
        Assert.Equal(OrganizationId, mapper.Map(Input())!.OrganizationUuid);
        organization.FkOrgUsersConnected = false;
        Assert.Null(mapper.Map(Input()));
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
        var registeredOrganizations = new Mock<IGenericRepository<Organization>>();
        registeredOrganizations.Setup(x => x.AsQueryable()).Returns(new[]
        {
            new Organization { Uuid = OrganizationId, Cvr = "12345678", FkOrgUsersConnected = true }
        }.AsQueryable());
        var job = new BeskedfordelerJob(Reader(), new OrgBrugerDeletionMapper(registeredOrganizations.Object),
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
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("  ,  ", 0)]
    [InlineData("ABC", 1)]
    [InlineData(" ABC , DEF ", 2)]
    public void Certificate_AppSetting_ParsesFingerprintList(string? value, int expectedCount)
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["AppSettings:BeskedfordelerCertificateSha256"] = value }).Build();
        var fingerprints = BeskedfordelerClientCertificateValidator.GetAllowedFingerprints(configuration);
        Assert.Equal(expectedCount, fingerprints.Length);
        if (expectedCount > 0) Assert.Equal("ABC", fingerprints[0]);
        if (expectedCount > 1) Assert.Equal("DEF", fingerprints[1]);
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
            new Dictionary<string, string?> { ["AppSettings:BeskedfordelerCertificateSha256"] = certificate.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256) }).Build();
        Assert.False(new BeskedfordelerClientCertificateValidator(configuration).IsValid(certificate));
        Assert.False(new BeskedfordelerClientCertificateValidator(configuration).IsValid(null));
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
        new(new BeskedfordelerService(Reader(), client))
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
