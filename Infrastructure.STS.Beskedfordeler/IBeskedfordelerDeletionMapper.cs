using Core.ApplicationServices.Model.Users;
using System.Xml.Linq;

namespace Infrastructure.STS.Beskedfordeler;

/// <summary>
/// Maps confirmed immediate ORG_Bruger deactivations to administrator-reviewed deletions.
/// Municipality CVRs resolve to organizations that have signed up for FK Organisation user synchronization in KITOS.
/// The receiver assigns ExternalMessageId from BeskedId after mapping.
/// </summary>
public interface IBeskedfordelerDeletionMapper
{
    ExternalUserChangeInput? Map(XElement validatedInput);
}
