using Core.ApplicationServices.Authorization;
using Core.DomainModel;
using Core.DomainModel.Organization;
using System.Linq;

namespace Core.ApplicationServices.Model.Users
{
    public class UserCollectionEditPermissionsResult
    {
        public UserCollectionEditPermissionsResult(bool editProperties, bool editContractRole, bool editSystemRole, bool editOrganizationRole)
        {
            EditProperties = editProperties;
            EditContractRole = editContractRole;
            EditSystemRole = editSystemRole;
            EditOrganizationRole = editOrganizationRole;

            CanEditAny = editProperties || editContractRole || editSystemRole || editOrganizationRole;
        }

        public bool CanEditAny { get; }
        public bool EditProperties { get; }
        public bool EditContractRole { get; }
        public bool EditSystemRole { get; }
        public bool EditOrganizationRole { get; }

        public static UserCollectionEditPermissionsResult FromOrganization(
            Organization organization,
            User user,
            IAuthorizationContext authorizationContext)
        {
            var modify = authorizationContext.AllowModify(organization);
            var roles = user.GetRolesInOrganization(organization.Uuid).ToList();

            if (modify && (roles.Contains(OrganizationRole.LocalAdmin) || user.IsGlobalAdmin))
            {
                return new UserCollectionEditPermissionsResult(true, true, true, true);
            }

            var hasContractRole = roles.Contains(OrganizationRole.ContractModuleAdmin);
            var hasSystemRole = roles.Contains(OrganizationRole.SystemModuleAdmin);
            var hasOrganizationRole = roles.Contains(OrganizationRole.OrganizationModuleAdmin);

            return new UserCollectionEditPermissionsResult(modify, hasContractRole, hasSystemRole, hasOrganizationRole);
        }
    }
}
