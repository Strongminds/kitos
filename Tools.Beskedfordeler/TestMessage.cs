using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Tools.Beskedfordeler;

public static class TestMessage
{
    public static readonly Guid MessageType = new("94ef3c82-10e9-4d12-a9d9-7a7242d5ba83");
    public static readonly Guid ObjectType = new("2b808769-8d09-4aa3-ac28-f8ac3526010f");
    public static readonly Guid DeactivationAction = new("9edb0c34-36e2-4d87-aba8-3ca7ade718ee");
    public static readonly XNamespace Envelope = "urn:oio:besked:kuvert:1.0";
    public static readonly XNamespace Sagdok = "urn:oio:sagdok:3.0.0";

    public static XElement Create(Guid senderId, string cvr, Guid userId)
    {
        ValidateIdentifiers(senderId, cvr, userId);

        return CreateMessage(senderId, cvr, userId);
    }

    private static void ValidateIdentifiers(Guid senderId, string cvr, Guid userId)
    {
        if (senderId == Guid.Empty)
            throw new ArgumentException("Sender UUID must not be empty.", nameof(senderId));
        if (userId == Guid.Empty)
            throw new ArgumentException("Affected FK Bruger UUID must not be empty.", nameof(userId));
        if (string.IsNullOrEmpty(cvr) || cvr.Length != 8 || cvr.Any(c => c is < '0' or > '9'))
            throw new ArgumentException("CVR must contain exactly eight digits.", nameof(cvr));
    }

    private static XElement CreateMessage(Guid senderId, string cvr, Guid userId)
    {
        var messageId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        XElement Id(string name, Guid value) =>
            new(Envelope + name, new XElement(Sagdok + "UUIDIdentifikator", value.ToString("D")));
        XElement Time(string name) =>
            new(Envelope + name, new XElement(Sagdok + "TidsstempelDatoTid",
                timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)));
        XElement Municipality(string name) =>
            new(Envelope + name, new XElement(Sagdok + "URNIdentifikator", $"urn:oio:cvr-nr:{cvr}"));

        return new XElement(Envelope + "Haendelsesbesked",
            new XAttribute(XNamespace.Xmlns + "besked", Envelope),
            new XAttribute(XNamespace.Xmlns + "sd", Sagdok),
            Id("BeskedId", messageId),
            new XElement(Envelope + "BeskedVersion", "1.0"),
            new XElement(Envelope + "Beskedkuvert",
                new XElement(Envelope + "Filtreringsdata",
                    Id("Beskedtype", MessageType),
                    Id("BeskedAnsvarligAktoer", senderId),
                    Municipality("TilladtModtager"),
                    new XElement(Envelope + "ObjektRegistrering",
                        Id("ObjektRegistreringId", registrationId),
                        Id("RegistreringsAktoer", senderId),
                        Time("Registreringstidspunkt"),
                        Municipality("ObjektAnsvarligMyndighed"),
                        Id("ObjektId", userId),
                        Id("ObjektType", ObjectType),
                        Id("ObjektHandling", DeactivationAction))),
                new XElement(Envelope + "Leveranceinformation",
                    Time("Dannelsestidspunkt")),
                Time("GyldighedFra")),
            CreateAuditPayload(senderId, messageId));
    }

    private static XElement CreateAuditPayload(Guid senderId, Guid messageId)
    {
        var audit = new XElement("audit",
            new XElement("Transport", "AMQP"),
            new XElement("Operation", "BrugerNedlagt"),
            new XElement("TransactionID", messageId.ToString("D")),
            new XElement("Bruger", senderId.ToString("D")));
        var bytes = Encoding.UTF8.GetBytes(audit.ToString(SaveOptions.DisableFormatting));
        return new XElement(Envelope + "Beskeddata",
            new XElement(Envelope + "Base64", new XAttribute("encoding", "UTF-8"),
                Convert.ToBase64String(bytes)));
    }

    public static int ReadStatus(string response)
    {
        using var input = new StringReader(response);
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1024 * 1024
        });
        var root = XElement.Load(reader);
        if (root.Name != Sagdok + "StandardRetur")
            throw new XmlException("Expected a KOMBIT StandardRetur response.");
        var status = root.Element(Sagdok + "StatusKode")?.Value;
        return int.TryParse(status, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code)
            ? code
            : throw new XmlException("Missing or invalid KOMBIT status code.");
    }
}
