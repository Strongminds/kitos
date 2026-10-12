using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.EntityFrameworkCore;
using PubSub.Application.Api.UserSync;
using PubSub.Application.Api.UserSync.Beskedfordeler;
using PubSub.Infrastructure.DataAccess;

namespace PubSub.Test.Unit.Application.Api;

public class OrgBrugerDeletionMapperTest
{
    private static readonly Guid OrganizationId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid MessageId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static OrgBrugerDeletionMapper Mapper() => new(new Dictionary<string, Guid> { ["12345678"] = OrganizationId });

    [Fact]
    public async Task Receive_OfficialSchemaDeactivation_PersistsMappedEventAndDeduplicates()
    {
        var schemas = BeskedfordelerXmlReader.LoadSchemas(Path.Combine(AppContext.BaseDirectory, "UserSync", "Beskedfordeler", "Schemas"));
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(Input().ToString()));
        var input = await new BeskedfordelerXmlReader(schemas).Read(body, default);
        var mapped = Assert.IsType<PubSub.Core.DomainModel.UserSync.UserDeletionEvent>(Mapper().Map(input));
        Assert.Equal(OrganizationId, mapped.OrganizationUuid);
        Assert.Equal(UserId, mapped.ExternalUserUuid);
        Assert.Equal(1, mapped.ChangeType);
        Assert.Equal(new DateTime(2023, 6, 15, 14, 41, 34, DateTimeKind.Utc), mapped.OccurredAt);
        Assert.Equal(DateTimeKind.Utc, mapped.OccurredAt!.Value.Kind);
        await using var db = new PubSubContext(new DbContextOptionsBuilder<PubSubContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var receiver = new BeskedfordelerReceiver(Mapper(), new UserChangeOutbox(db));
        Assert.Equal(BeskedfordelerStatus.Accepted, await receiver.Receive(input, default));
        Assert.Equal(BeskedfordelerStatus.Accepted, await receiver.Receive(input, default));
        Assert.Equal(MessageId.ToString(), Assert.Single(db.UserChangeDeliveries).ExternalMessageId);
        // Validate the actual protocol reply against the official callback schema too.
        XDocument.Parse(BeskedfordelerResponse.Serialize(BeskedfordelerStatus.Accepted)).Validate(schemas, (_, e) => throw e.Exception);
    }

    [Theory]
    [InlineData("Beskedtype", "00000000-0000-0000-0000-000000000001")]
    [InlineData("ObjektType", "ce7fcf97-a8a2-447a-8690-d38aa0f6e23c")]
    [InlineData("ObjektHandling", "b91ebc7e-7540-44a0-a360-ea58a3adc0e2")]
    [InlineData("ObjektHandling", "588a59a3-6ee4-4b6f-b423-5b082f5d290e")]
    [InlineData("ObjektHandling", "6ce72dab-1e09-425e-8503-5d6522f72fdf")]
    [InlineData("ObjektId", "00000000-0000-0000-0000-000000000000")]
    [InlineData("ObjektId", "not-a-uuid")]
    [InlineData("ObjektAnsvarligMyndighed", "urn:oio:cvr-nr:87654321")]
    [InlineData("ObjektAnsvarligMyndighed", "urn:oio:cvr-nr:1234567")]
    [InlineData("ObjektAnsvarligMyndighed", "urn:oio:cvr-nr:urn:oio:cvr:12345678")]
    [InlineData("TilladtModtager", "urn:oio:cvr-nr:87654321")]
    [InlineData("TilladtModtager", "*")]
    [InlineData("Registreringstidspunkt", "2023-06-15T16:41:34")]
    [InlineData("Registreringstidspunkt", "invalidZ")]
    public async Task Receive_UnsupportedOrUnresolvedEvent_DoesNotPersistOrAcknowledge(string field, string value)
    {
        var input = Input();
        input.Descendants(BeskedfordelerResponse.Envelope + field).Single().Elements().Single().Value = value;
        await using var db = new PubSubContext(new DbContextOptionsBuilder<PubSubContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Assert.Equal(BeskedfordelerStatus.NotImplemented,
            await new BeskedfordelerReceiver(Mapper(), new UserChangeOutbox(db)).Receive(input, default));
        Assert.Empty(db.UserChangeDeliveries);
    }

    [Theory]
    [InlineData("Beskedtype")]
    [InlineData("ObjektType")]
    [InlineData("ObjektHandling")]
    [InlineData("ObjektId")]
    [InlineData("ObjektAnsvarligMyndighed")]
    [InlineData("Registreringstidspunkt")]
    [InlineData("TilladtModtager")]
    public void Map_MissingProducerField_Rejects(string field)
    {
        var input = Input();
        input.Descendants(BeskedfordelerResponse.Envelope + field).Single().Remove();
        Assert.Null(Mapper().Map(input));
    }

    [Fact]
    public void Map_MultipleRegistrations_RejectsInsteadOfDroppingEvents()
    {
        var input = Input();
        var registration = input.Descendants(BeskedfordelerResponse.Envelope + "ObjektRegistrering").Single();
        registration.AddAfterSelf(new XElement(registration));
        Assert.Null(Mapper().Map(input));
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("urn:oio:cvr:12345678")]
    [InlineData("urn:oio:cvr-nr:12345678")]
    public void Map_RecipientList_UsesResponsibleAuthorityNotFirstRecipient(string cvr)
    {
        var input = Input();
        var recipient = input.Descendants(BeskedfordelerResponse.Envelope + "TilladtModtager").Single();
        recipient.Elements().Single().Value = cvr;
        var other = new XElement(recipient);
        other.Elements().Single().Value = "urn:oio:cvr-nr:87654321";
        recipient.AddBeforeSelf(other);
        input.Descendants(BeskedfordelerResponse.Envelope + "ObjektAnsvarligMyndighed").Single().Elements().Single().Value = cvr;
        Assert.Equal(OrganizationId, Mapper().Map(input)?.OrganizationUuid);
    }

    [Fact]
    public void Constructor_InvalidOrAmbiguousOrganizationConfiguration_Rejects()
    {
        Assert.Throws<ArgumentException>(() => new OrgBrugerDeletionMapper(new Dictionary<string, Guid>()));
        Assert.Throws<ArgumentException>(() => new OrgBrugerDeletionMapper(new Dictionary<string, Guid> { ["invalid"] = OrganizationId }));
        Assert.Throws<ArgumentException>(() => new OrgBrugerDeletionMapper(new Dictionary<string, Guid> { ["12345678"] = Guid.Empty }));
        Assert.Throws<ArgumentException>(() => new OrgBrugerDeletionMapper(new Dictionary<string, Guid>
        {
            ["12345678"] = OrganizationId,
            ["87654321"] = OrganizationId
        }));
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
