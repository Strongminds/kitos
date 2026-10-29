using Kitos.Integration.Contracts;
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure;

public class BeskedfordelerWorkerTest
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static IExternalUserChangeIngestionTransaction IngestionTransaction()
    {
        var transaction = new Mock<IExternalUserChangeIngestionTransaction>();
        transaction.Setup(x => x.Execute(It.IsAny<Func<Core.Abstractions.Types.Result<ExternalUserChange, Core.Abstractions.Types.OperationError>>>()))
            .Returns((Func<Core.Abstractions.Types.Result<ExternalUserChange, Core.Abstractions.Types.OperationError>> action) => action());
        return transaction.Object;
    }
    private static BeskedfordelerXmlReader Reader() => new(BeskedfordelerXmlReader.LoadSchemas());

    [Theory]
    [InlineData(typeof(IBeskedfordelerJob))]
    [InlineData(typeof(BeskedfordelerJob))]
    [InlineData(typeof(global::Presentation.Web.Infrastructure.Beskedfordeler.BeskedfordelerJob))]
    public void Retry_QuarantineRemainsFailed_TransientFailureRetriesAndExhausts(Type jobType)
    {
        var method = jobType.GetMethod(nameof(IBeskedfordelerJob.Process))!;
        var job = new Job(jobType, method, "original-payload", CancellationToken.None);
        var filters = new JobFilterAttributeFilterProvider().GetFilters(job).Select(x => x.Instance).ToArray();
        var retry = Assert.Single(filters.OfType<AutomaticRetryAttribute>());
        var quarantine = new FailedState(new BeskedfordelerQuarantineException("Mapping/MunicipalityNotConnected"));
        var quarantined = Election(job, quarantine, 0);
        retry.OnStateElection(quarantined);
        Assert.Same(quarantine, quarantined.CandidateState);

        var transient = Election(job, new FailedState(new TimeoutException()), 0);
        retry.OnStateElection(transient);
        Assert.IsType<ScheduledState>(transient.CandidateState);

        var exhausted = new FailedState(new TimeoutException());
        var finalAttempt = Election(job, exhausted, retry.Attempts);
        retry.OnStateElection(finalAttempt);
        Assert.Same(exhausted, finalAttempt.CandidateState);
    }

    private static ElectStateContext Election(Job job, FailedState failed, int retryCount)
    {
        var connection = new Mock<Hangfire.Storage.IStorageConnection>();
        connection.Setup(x => x.GetJobParameter("job", "RetryCount")).Returns(retryCount.ToString());
        var applied = new ApplyStateContext(Mock.Of<JobStorage>(), connection.Object,
            Mock.Of<Hangfire.Storage.IWriteOnlyTransaction>(),
            new BackgroundJob("job", job, DateTime.UtcNow), failed, ProcessingState.StateName);
        return new ElectStateContext(applied);
    }

    [Theory]
    [InlineData("Beskedtype", BeskedfordelerMappingFailure.UnsupportedMessageType)]
    [InlineData("ObjektType", BeskedfordelerMappingFailure.UnsupportedObjectType)]
    [InlineData("ObjektHandling", BeskedfordelerMappingFailure.UnsupportedAction)]
    [InlineData("ObjektId", BeskedfordelerMappingFailure.InvalidUserIdentifier)]
    [InlineData("ObjektAnsvarligMyndighed", BeskedfordelerMappingFailure.InvalidMunicipalityCvr)]
    [InlineData("TilladtModtager", BeskedfordelerMappingFailure.RecipientMismatch)]
    [InlineData("Registreringstidspunkt", BeskedfordelerMappingFailure.InvalidTimestamp)]
    public void Map_InvalidField_ReturnsSpecificReason(string field, BeskedfordelerMappingFailure reason)
    {
        var xml = Input();
        xml.Descendants(BeskedfordelerMessage.Envelope + field).Single().Elements().Single().Value = "invalid";
        var repository = new Mock<IGenericRepository<Organization>>(MockBehavior.Strict);
        Assert.Equal(reason, new OrgBrugerDeletionMapper(repository.Object).Map(xml).Error);
        repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Map_UnsupportedRegistrationCount_ReturnsSpecificReason(int count)
    {
        var xml = Input();
        var registration = xml.Descendants(BeskedfordelerMessage.Envelope + "ObjektRegistrering").Single();
        if (count == 0) registration.Remove();
        else registration.AddAfterSelf(new XElement(registration));
        var repository = new Mock<IGenericRepository<Organization>>(MockBehavior.Strict);
        Assert.Equal(BeskedfordelerMappingFailure.UnsupportedRegistrationCount,
            new OrgBrugerDeletionMapper(repository.Object).Map(xml).Error);
    }

    [Theory]
    [InlineData("not base64")]
    [InlineData("PGludmFsaWQgLz4=")]
    public async Task Process_InvalidStoredPayload_QuarantinesWithoutDisclosingPayload(string payload)
    {
        var job = new BeskedfordelerJob(Reader(), Mock.Of<IBeskedfordelerDeletionMapper>(),
            new ExternalUserChangeIngestionService(Mock.Of<IExternalUserChangeRepository>(),
                Mock.Of<IOrganizationRepository>(), Mock.Of<ISsoUserIdentityRepository>(), IngestionTransaction()),
            NullLogger<BeskedfordelerJob>.Instance);
        var failure = await Assert.ThrowsAsync<BeskedfordelerQuarantineException>(() => job.Process(payload, default));
        Assert.Equal("Beskedfordeler quarantined: InvalidStoredPayload.", failure.Message);
        Assert.Null(failure.InnerException);
    }

    [Fact]
    public async Task Process_MunicipalityLookupTimesOut_PreservesTransientException()
    {
        var repository = new Mock<IGenericRepository<Organization>>();
        var timeout = new TimeoutException("Database unavailable");
        repository.Setup(x => x.AsQueryable()).Throws(timeout);
        var job = new BeskedfordelerJob(Reader(), new OrgBrugerDeletionMapper(repository.Object),
            new ExternalUserChangeIngestionService(Mock.Of<IExternalUserChangeRepository>(),
                Mock.Of<IOrganizationRepository>(), Mock.Of<ISsoUserIdentityRepository>(), IngestionTransaction()),
            NullLogger<BeskedfordelerJob>.Instance);
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Input().ToString()));
        Assert.Same(timeout, await Assert.ThrowsAsync<TimeoutException>(() => job.Process(payload, default)));
    }

    [Fact]
    public async Task Process_UserNotFound_QuarantinesWithoutCreatingPendingChange()
    {
        var registeredOrganizations = new Mock<IGenericRepository<Organization>>();
        var organization = new Organization { Uuid = OrganizationId, Cvr = "12345678", FkOrgUsersConnected = true };
        registeredOrganizations.Setup(x => x.AsQueryable()).Returns(new[] { organization }.AsQueryable());
        var organizations = new Mock<IOrganizationRepository>();
        organizations.Setup(x => x.GetByUuid(OrganizationId)).Returns(organization);
        var changes = new Mock<IExternalUserChangeRepository>();
        var identities = new Mock<ISsoUserIdentityRepository>();
        identities.Setup(x => x.GetByExternalUuid(UserId)).Returns(Core.Abstractions.Types.Maybe<SsoUserIdentity>.None);
        var job = new BeskedfordelerJob(Reader(), new OrgBrugerDeletionMapper(registeredOrganizations.Object),
            new ExternalUserChangeIngestionService(changes.Object, organizations.Object, identities.Object, IngestionTransaction()),
            NullLogger<BeskedfordelerJob>.Instance);
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Input().ToString()));
        var failure = await Assert.ThrowsAsync<BeskedfordelerQuarantineException>(() => job.Process(payload, default));
        Assert.Contains("Ingestion/NotFound", failure.Message);
        changes.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Never);
    }

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
            Assert.True(result.Ok);
            Assert.Equal(organizations[0].Uuid, result.Value.OrganizationUuid);
        }
        else Assert.Equal(matchingSignups == 0 ? BeskedfordelerMappingFailure.MunicipalityNotConnected : BeskedfordelerMappingFailure.AmbiguousMunicipality, result.Error);
    }

    [Fact]
    public void Map_ObservesSignupAndDisconnectWithoutRestart()
    {
        var organization = new Organization { Uuid = OrganizationId, Cvr = "12345678" };
        var repository = new Mock<IGenericRepository<Organization>>();
        repository.Setup(x => x.AsQueryable()).Returns(new[] { organization }.AsQueryable());
        var mapper = new OrgBrugerDeletionMapper(repository.Object);

        Assert.Equal(BeskedfordelerMappingFailure.MunicipalityNotConnected, mapper.Map(Input()).Error);
        organization.FkOrgUsersConnected = true;
        Assert.Equal(OrganizationId, mapper.Map(Input()).Value.OrganizationUuid);
        organization.FkOrgUsersConnected = false;
        Assert.Equal(BeskedfordelerMappingFailure.MunicipalityNotConnected, mapper.Map(Input()).Error);
    }

    [Theory]
    [InlineData("7654895c-a3f3-4c77-8afd-4c77330f963c", true)]
    [InlineData("7654895C-A3F3-4C77-8AFD-4C77330F963C", true)]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", false)]
    public async Task Process_PreviouslyQueuedTestMessage_LogsWarningWithoutUserChanges(string messageType, bool supported)
    {
        var xml = Input();
        xml.Descendants(BeskedfordelerMessage.Envelope + "Beskedtype").Single().Elements().Single().Value = messageType;
        var mapper = new Mock<IBeskedfordelerDeletionMapper>();
        mapper.Setup(x => x.Map(It.IsAny<XElement>())).Returns(BeskedfordelerMappingFailure.UnsupportedMessageType);
        var repository = new Mock<IExternalUserChangeRepository>(MockBehavior.Strict);
        var organizations = new Mock<IOrganizationRepository>(MockBehavior.Strict);
        var identities = new Mock<ISsoUserIdentityRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BeskedfordelerJob>>();
        var job = new BeskedfordelerJob(Reader(), mapper.Object,
            new ExternalUserChangeIngestionService(repository.Object, organizations.Object, identities.Object, IngestionTransaction()), logger.Object);
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml.ToString()));
        if (supported)
        {
            await job.Process(payload, default);
            mapper.VerifyNoOtherCalls();
            logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Test/TestbeskedType1") &&
                    state.ToString()!.Contains(MessageId.ToString()) && !state.ToString()!.Contains("<")),
                null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
        else
        {
            await Assert.ThrowsAsync<BeskedfordelerQuarantineException>(() => job.Process(payload, default));
            logger.VerifyNoOtherCalls();
        }
        repository.VerifyNoOtherCalls();
        organizations.VerifyNoOtherCalls();
        identities.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Process_ReplayPreservesOneEvent_ConflictsFailVisibly(bool conflict)
    {
        var organizations = new Mock<IOrganizationRepository>();
        organizations.Setup(x => x.GetByUuid(OrganizationId)).Returns(new Organization { Id = 1, Uuid = OrganizationId, FkOrgUsersConnected = true });
        var identities = new Mock<ISsoUserIdentityRepository>();
        var user = new Core.DomainModel.User { Id = 42 };
        user.OrganizationRights.Add(new OrganizationRight { OrganizationId = 1, Role = OrganizationRole.User });
        identities.Setup(x => x.GetByExternalUuid(UserId)).Returns(new SsoUserIdentity(UserId, user));
        var repository = new Mock<IExternalUserChangeRepository>();
        repository.Setup(x => x.AsQueryable()).Returns(Array.Empty<ExternalUserChange>().AsQueryable());
        ExternalUserChange? saved = null;
        repository.Setup(x => x.FindByMessageId(MessageId.ToString("D"))).Returns(() => saved);
        repository.Setup(x => x.InsertAndSave(It.IsAny<ExternalUserChange>())).Returns((ExternalUserChange value) => saved = value);
        var registeredOrganizations = new Mock<IGenericRepository<Organization>>();
        var signup = new Organization { Uuid = OrganizationId, Cvr = "12345678", FkOrgUsersConnected = false };
        registeredOrganizations.Setup(x => x.AsQueryable()).Returns(new[] { signup }.AsQueryable());
        var job = new BeskedfordelerJob(Reader(), new OrgBrugerDeletionMapper(registeredOrganizations.Object),
            new ExternalUserChangeIngestionService(repository.Object, organizations.Object, identities.Object, IngestionTransaction()),
            NullLogger<BeskedfordelerJob>.Instance);
        var xml = Input();
        string Payload() => Convert.ToBase64String(Encoding.UTF8.GetBytes(xml.ToString()));
        Task ExecuteSerializedJob()
        {
            var payload = Payload();
            var queued = Job.FromExpression<IBeskedfordelerJob>(worker => worker.Process(payload, CancellationToken.None));
            var restored = Hangfire.Storage.InvocationData.SerializeJob(queued).DeserializeJob();
            return Assert.IsAssignableFrom<Task>(restored.Method.Invoke(job, restored.Args.ToArray()));
        }
        var quarantined = await Assert.ThrowsAsync<BeskedfordelerQuarantineException>(ExecuteSerializedJob);
        Assert.Contains(nameof(BeskedfordelerMappingFailure.MunicipalityNotConnected), quarantined.Message);
        repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Never);
        signup.FkOrgUsersConnected = true;
        await ExecuteSerializedJob();
        Assert.Equal(ExternalUserChangeStatus.Pending, saved!.Status);
        Assert.Equal(user.Id, saved.UserId);
        if (conflict)
        {
            xml.Descendants(BeskedfordelerMessage.Envelope + "ObjektId").Single().Elements().Single().Value = Guid.NewGuid().ToString();
            await Assert.ThrowsAsync<BeskedfordelerQuarantineException>(ExecuteSerializedJob);
        }
        else await ExecuteSerializedJob();
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
