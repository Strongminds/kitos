using System.Xml.Linq;
using PubSub.Core.DomainModel.UserSync;

namespace PubSub.Application.Api.UserSync.Beskedfordeler;

/// <summary>
/// Implement only from an authoritative FK Organisation producer contract.
/// Envelope actor/object identifiers are not necessarily KITOS organization/SAML identifiers.
/// The receiver assigns ExternalMessageId from BeskedId after mapping.
/// </summary>
public interface IBeskedfordelerDeletionMapper
{
    UserDeletionEvent? Map(XElement validatedInput);
}

public sealed class UnsupportedBeskedfordelerDeletionMapper : IBeskedfordelerDeletionMapper
{
    public UserDeletionEvent? Map(XElement validatedInput) => null;
}
