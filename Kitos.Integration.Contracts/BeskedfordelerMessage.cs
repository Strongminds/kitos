using System.Xml.Linq;

namespace Kitos.Integration.Contracts;

public static class BeskedfordelerMessage
{
    public static readonly XNamespace Sts = "urn:oio:sts:1.0.0";
    public static readonly XNamespace Envelope = "urn:oio:besked:kuvert:1.0";
    public static readonly XNamespace Sagdok = "urn:oio:sagdok:3.0.0";
}
