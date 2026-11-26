using Kitos.Integration.Contracts;
using Core.Abstractions.Types;
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
    private const int TimezoneSuffixLength = 6;
    private const int TimezoneSeparatorOffset = 3;
    private const int AmbiguousMatchLimit = 2;
    private const int CvrLength = 8;

    public Result<ExternalUserChangeInput, BeskedfordelerMappingFailure> Map(XElement validatedInput)
    {
        var envelope = BeskedfordelerMessage.Envelope;
        var message = validatedInput.Element(envelope + "Haendelsesbesked");
        var filter = message?.Element(envelope + "Beskedkuvert")?.Element(envelope + "Filtreringsdata");
        if (ReadUuid(filter, "Beskedtype") != MessageType) return BeskedfordelerMappingFailure.UnsupportedMessageType;
        var registrations = filter?.Elements(envelope + "ObjektRegistrering").ToArray() ?? [];
        // One source message produces one preserved event. Never silently drop additional registrations.
        if (registrations.Length != 1) return BeskedfordelerMappingFailure.UnsupportedRegistrationCount;
        var registration = registrations[0];
        if (ReadUuid(registration, "ObjektType") != UserObjectType) return BeskedfordelerMappingFailure.UnsupportedObjectType;
        if (ReadUuid(registration, "ObjektHandling") != UserDeactivated) return BeskedfordelerMappingFailure.UnsupportedAction;

        var userId = ReadUuid(registration, "ObjektId");
        var cvr = ReadCvr(registration.Element(envelope + "ObjektAnsvarligMyndighed"));
        if (userId == Guid.Empty) return BeskedfordelerMappingFailure.InvalidUserIdentifier;
        if (cvr == null) return BeskedfordelerMappingFailure.InvalidMunicipalityCvr;
        // SF1461 2.0 delivers an explicit recipient list; it is not an organization identifier.
        if (filter == null || !filter.Elements(envelope + "TilladtModtager").Any(x => ReadCvr(x) == cvr)) return BeskedfordelerMappingFailure.RecipientMismatch;

        return ReadOccurrenceTime(registration).Bind(occurredAt =>
            ResolveOrganization(cvr).Select(organizationUuid =>
                // ObjektId identifies the affected user; Beskeddata/Bruger identifies the actor.
                new ExternalUserChangeInput("assigned-by-receiver", organizationUuid, userId,
                    Core.DomainModel.Users.ExternalUserChangeType.Deleted, occurredAt)));
    }

    private static Result<DateTime, BeskedfordelerMappingFailure> ReadOccurrenceTime(XElement registration)
    {
        var timestamp = registration.Element(BeskedfordelerMessage.Envelope + "Registreringstidspunkt")?
            .Element(BeskedfordelerMessage.Sagdok + "TidsstempelDatoTid")?.Value;
        if (timestamp == null || !(timestamp.EndsWith('Z') ||
            (timestamp.Length >= TimezoneSuffixLength && timestamp[^TimezoneSeparatorOffset] == ':' &&
                timestamp[^TimezoneSuffixLength] is '+' or '-'))) return BeskedfordelerMappingFailure.InvalidTimestamp;
        try
        {
            return XmlConvert.ToDateTimeOffset(timestamp).UtcDateTime;
        }
        catch (FormatException)
        {
            return BeskedfordelerMappingFailure.InvalidTimestamp;
        }
    }

    private Result<Guid, BeskedfordelerMappingFailure> ResolveOrganization(string cvr)
    {
        // Signup in KITOS is the source of truth. Resolve afresh for every delivery/retry.
        var matches = organizations.AsQueryable()
            .Where(organization => organization.Cvr == cvr && organization.FkOrgUsersConnected)
            .Select(organization => organization.Uuid)
            .Take(AmbiguousMatchLimit)
            .ToArray();
        if (matches.Length == 0) return BeskedfordelerMappingFailure.MunicipalityNotConnected;
        if (matches.Length > 1) return BeskedfordelerMappingFailure.AmbiguousMunicipality;
        if (matches[0] == Guid.Empty) return BeskedfordelerMappingFailure.InvalidOrganizationIdentifier;

        return matches[0];
    }

    private static Guid ReadUuid(XElement? parent, string field) =>
        Guid.TryParse(parent?.Element(BeskedfordelerMessage.Envelope + field)?
            .Element(BeskedfordelerMessage.Sagdok + "UUIDIdentifikator")?.Value, out var value) ? value : Guid.Empty;

    private static string? ReadCvr(XElement? identifier)
    {
        var value = identifier?.Element(BeskedfordelerMessage.Sagdok + "URNIdentifikator")?.Value;
        if (value == null) return null;
        foreach (var prefix in new[] { "urn:oio:cvr-nr:", "urn:oio:cvr:" })
            if (value.StartsWith(prefix, StringComparison.Ordinal))
            {
                value = value[prefix.Length..];
                break;
            }
        return IsCvr(value) ? value : null;
    }

    private static bool IsCvr(string value) => value.Length == CvrLength && value.All(char.IsAsciiDigit);
}
