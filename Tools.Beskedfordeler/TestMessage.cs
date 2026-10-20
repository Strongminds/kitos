using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Tools.Beskedfordeler;

public static class TestMessage
{
    public static readonly Guid MessageType = new("7654895c-a3f3-4c77-8afd-4c77330f963c");
    public static readonly XNamespace Envelope = "urn:oio:besked:kuvert:1.0";
    public static readonly XNamespace Sagdok = "urn:oio:sagdok:3.0.0";

    public static XElement Create(Guid senderId, string cvr)
    {
        if (senderId == Guid.Empty)
            throw new ArgumentException("Sender UUID must not be empty.", nameof(senderId));
        if (cvr.Length != 8 || cvr.Any(c => c is < '0' or > '9'))
            throw new ArgumentException("CVR must contain exactly eight digits.", nameof(cvr));

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
                        Municipality("ObjektAnsvarligMyndighed"))),
                new XElement(Envelope + "Leveranceinformation",
                    Time("Dannelsestidspunkt")),
                Time("GyldighedFra")),
            new XElement(Envelope + "Beskeddata",
                new XElement(XName.Get("Beskeddata", "urn:kombit:test"),
                    $"KITOS Test/TestbeskedType1 {messageId:D}")));
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
