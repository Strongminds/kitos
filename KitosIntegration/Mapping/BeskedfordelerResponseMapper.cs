using Kitos.Integration.Contracts;
using System.Xml.Linq;

namespace KitosIntegration.Mapping;

public static class BeskedfordelerResponseMapper
{

    public static string Serialize(BeskedfordelerStatus status) =>
        new XElement(BeskedfordelerMessage.Sts + "ModtagBeskedOutput",
            new XElement(BeskedfordelerMessage.Sagdok + "StandardRetur",
                new XElement(BeskedfordelerMessage.Sagdok + "StatusKode", (int)status),
                new XElement(BeskedfordelerMessage.Sagdok + "FejlbeskedTekst", status switch
                {
                    BeskedfordelerStatus.Accepted => "",
                    BeskedfordelerStatus.CannotReceive => "Besked kan ikke modtages",
                    BeskedfordelerStatus.NotImplemented => "Service ikke implementeret",
                    BeskedfordelerStatus.Unavailable => "Service ikke tilgængelig",
                    _ => "Service version ikke understøttet"
                }))).ToString(SaveOptions.DisableFormatting);
}
