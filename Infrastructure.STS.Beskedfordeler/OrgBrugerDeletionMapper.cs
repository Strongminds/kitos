using System;
using System.Linq;
using Core.ApplicationServices.Model.Users;
using Core.DomainModel.Organization;
using Core.DomainServices;
using System.Xml;
using System.Xml.Linq;

namespace Infrastructure.STS.Beskedfordeler;

/// <summary>SF1500 ORG_Bruger 1.0, sections 1-3. Slet and Passiver do not emit events.</summary>
public sealed class OrgBrugerDeletionMapper(IGenericRepository<Organization> organizations) : IBeskedfordelerDeletionMapper
{
    public static readonly Guid MessageType = new("94ef3c82-10e9-4d12-a9d9-7a7242d5ba83");
    public static readonly Guid UserObjectType = new("2b808769-8d09-4aa3-ac28-f8ac3526010f");
    public static readonly Guid UserDeactivated = new("9edb0c34-36e2-4d87-aba8-3ca7ade718ee");

    public ExternalUserChangeInput? Map(XElement validatedInput)
    {
        var envelope = BeskedfordelerResponse.Envelope;
        var sagdok = BeskedfordelerResponse.Sagdok;
        var message = validatedInput.Element(envelope + "Haendelsesbesked");
        var filter = message?.Element(envelope + "Beskedkuvert")?.Element(envelope + "Filtreringsdata");
        if (ReadUuid(filter, "Beskedtype") != MessageType) return null;
        var registrations = filter?.Elements(envelope + "ObjektRegistrering").ToArray() ?? [];
        // One source message produces one preserved event. Never silently drop additional registrations.
        if (registrations.Length != 1) return null;
        var registration = registrations[0];
        if (ReadUuid(registration, "ObjektType") != UserObjectType ||
            ReadUuid(registration, "ObjektHandling") != UserDeactivated) return null;

        var userId = ReadUuid(registration, "ObjektId");
        var cvr = ReadCvr(registration.Element(envelope + "ObjektAnsvarligMyndighed"));
        if (userId == Guid.Empty || cvr == null) return null;
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

        // Signup in KITOS is the source of truth. Resolve afresh for every delivery/retry.
        var matches = organizations.AsQueryable()
            .Where(organization => organization.Cvr == cvr && organization.FkOrgUsersConnected)
            .Select(organization => organization.Uuid)
            .Take(2)
            .ToArray();
        if (matches.Length != 1 || matches[0] == Guid.Empty) return null;

        // KITOS SSO reads FK Bruger using the SAML UUID and stores that UUID as SsoUserIdentity.ExternalUuid.
        // Beskeddata/Bruger identifies the actor making the change, not the affected user.
        return new ExternalUserChangeInput("assigned-by-receiver", matches[0], userId, Core.DomainModel.Users.ExternalUserChangeType.Deleted, occurredAt);
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
