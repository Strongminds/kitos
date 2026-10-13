using System.Xml.Linq;

namespace PubSub.Application.Api.UserSync.Beskedfordeler;

public enum BeskedfordelerStatus
{
    Accepted = 20,
    CannotReceive = 40,
    NotImplemented = 51,
    Unavailable = 53,
    UnsupportedVersion = 55
}

public static class BeskedfordelerResponse
{
    public static readonly XNamespace Sts = "urn:oio:sts:1.0.0";
    public static readonly XNamespace Envelope = "urn:oio:besked:kuvert:1.0";
    public static readonly XNamespace Sagdok = "urn:oio:sagdok:3.0.0";

    public static string Serialize(BeskedfordelerStatus status) =>
        new XElement(Sts + "ModtagBeskedOutput",
            new XElement(Sagdok + "StandardRetur",
                new XElement(Sagdok + "StatusKode", (int)status),
                new XElement(Sagdok + "FejlbeskedTekst", status switch
                {
                    BeskedfordelerStatus.Accepted => "",
                    BeskedfordelerStatus.CannotReceive => "Besked kan ikke modtages",
                    BeskedfordelerStatus.NotImplemented => "Service ikke implementeret",
                    BeskedfordelerStatus.Unavailable => "Service ikke tilgængelig",
                    _ => "Service version ikke understøttet"
                }))).ToString(SaveOptions.DisableFormatting);
}
