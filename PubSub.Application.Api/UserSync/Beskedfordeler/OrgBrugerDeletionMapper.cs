using UserSync.Transport;
using System.Xml;
using System.Xml.Linq;
using PubSub.Core.DomainModel.UserSync;

namespace PubSub.Application.Api.UserSync.Beskedfordeler;

/// <summary>SF1500 ORG_Bruger 1.0, sections 1-3. Slet and Passiver do not emit events.</summary>
public sealed class OrgBrugerDeletionMapper : IBeskedfordelerDeletionMapper
{
    public static readonly Guid MessageType = new("94ef3c82-10e9-4d12-a9d9-7a7242d5ba83");
    public static readonly Guid UserObjectType = new("2b808769-8d09-4aa3-ac28-f8ac3526010f");
    public static readonly Guid UserDeactivated = new("9edb0c34-36e2-4d87-aba8-3ca7ade718ee");
    private readonly Dictionary<string, Guid> _organizations;

    public OrgBrugerDeletionMapper(IReadOnlyDictionary<string, Guid> organizations)
    {
        if (organizations.Count == 0 || organizations.Any(x => !IsCvr(x.Key) || x.Value == Guid.Empty) ||
            organizations.Values.Distinct().Count() != organizations.Count)
            throw new ArgumentException("Configure unique eight-digit CVRs and non-empty KITOS organization UUIDs.", nameof(organizations));
        _organizations = new Dictionary<string, Guid>(organizations, StringComparer.Ordinal);
    }

    public UserDeletionEvent? Map(XElement validatedInput)
    {
        var envelope = BeskedfordelerResponse.Envelope;
        var sagdok = BeskedfordelerResponse.Sagdok;
        var message = validatedInput.Element(envelope + "Haendelsesbesked");
        var filter = message?.Element(envelope + "Beskedkuvert")?.Element(envelope + "Filtreringsdata");
        if (ReadUuid(filter, "Beskedtype") != MessageType) return null;
        var registrations = filter?.Elements(envelope + "ObjektRegistrering").ToArray() ?? [];
        // One source message currently produces one outbox event. Never silently drop additional registrations.
        if (registrations.Length != 1) return null;
        var registration = registrations[0];
        if (ReadUuid(registration, "ObjektType") != UserObjectType ||
            ReadUuid(registration, "ObjektHandling") != UserDeactivated) return null;

        var userId = ReadUuid(registration, "ObjektId");
        var cvr = ReadCvr(registration.Element(envelope + "ObjektAnsvarligMyndighed"));
        if (userId == Guid.Empty || cvr == null || !_organizations.TryGetValue(cvr, out var organizationId)) return null;
        // SF1461 2.0 delivers an explicit recipient list; it is not an organization identifier.
        if (filter == null || !filter.Elements(envelope + "TilladtModtager").Any(x => ReadCvr(x) == cvr)) return null;

        var timestamp = registration.Element(envelope + "Registreringstidspunkt")?.Element(sagdok + "TidsstempelDatoTid")?.Value;
        if (timestamp == null || !(timestamp.EndsWith('Z') ||
            (timestamp.Length >= 6 && timestamp[^3] == ':' && timestamp[^6] is '+' or '-'))) return null;
        DateTime occurredAt;
        try
        {
            occurredAt = XmlConvert.ToDateTimeOffset(timestamp).UtcDateTime;
        }
        catch (FormatException)
        {
            return null;
        }

        // KITOS SSO reads FK Bruger using the SAML UUID and stores that UUID as SsoUserIdentity.ExternalUuid.
        // Beskeddata/Bruger identifies the actor making the change, not the affected user.
        return new UserDeletionEvent("assigned-by-receiver", organizationId, userId, OccurredAt: occurredAt);
    }

    private static Guid ReadUuid(XElement? parent, string field) =>
        Guid.TryParse(parent?.Element(BeskedfordelerResponse.Envelope + field)?
            .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")?.Value, out var value) ? value : Guid.Empty;

    private static string? ReadCvr(XElement? identifier)
    {
        var value = identifier?.Element(BeskedfordelerResponse.Sagdok + "URNIdentifikator")?.Value;
        if (value == null) return null;
        foreach (var prefix in new[] { "urn:oio:cvr-nr:", "urn:oio:cvr:" })
            if (value.StartsWith(prefix, StringComparison.Ordinal))
            {
                value = value[prefix.Length..];
                break;
            }
        return IsCvr(value) ? value : null;
    }

    private static bool IsCvr(string value) => value.Length == 8 && value.All(char.IsAsciiDigit);
}
