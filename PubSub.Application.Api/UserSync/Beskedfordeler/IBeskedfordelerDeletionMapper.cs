using System.Xml.Linq;
using PubSub.Core.DomainModel.UserSync;

namespace PubSub.Application.Api.UserSync.Beskedfordeler;

/// <summary>
/// Maps confirmed immediate ORG_Bruger deactivations to administrator-reviewed deletions.
/// Municipality CVRs must be resolved to explicitly configured KITOS organization UUIDs.
/// The receiver assigns ExternalMessageId from BeskedId after mapping.
/// </summary>
public interface IBeskedfordelerDeletionMapper
{
    UserDeletionEvent? Map(XElement validatedInput);
}
