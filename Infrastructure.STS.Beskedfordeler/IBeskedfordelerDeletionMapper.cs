using Core.ApplicationServices.Model.Users;
using System.Xml.Linq;

namespace Infrastructure.STS.Beskedfordeler;

/// <summary>
/// Maps confirmed immediate ORG_Bruger deactivations to administrator-reviewed deletions.
/// Municipality CVRs must be resolved to explicitly configured KITOS organization UUIDs.
/// The receiver assigns ExternalMessageId from BeskedId after mapping.
/// </summary>
public interface IBeskedfordelerDeletionMapper
{
    ExternalUserChangeInput? Map(XElement validatedInput);
}
