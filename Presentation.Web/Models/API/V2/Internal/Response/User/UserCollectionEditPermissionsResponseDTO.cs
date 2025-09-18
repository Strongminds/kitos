namespace Presentation.Web.Models.API.V2.Internal.Response.User
{
    public class UserCollectionEditPermissionsResponseDTO
    {
        public UserCollectionEditPermissionsResponseDTO(bool edit, bool modifyProperties, bool modifyContractRole, bool modifySystemRole, bool modifyOrganizationRole)
        {
            ModifyProperties = modifyProperties;
            ModifyContractRole = modifyContractRole;
            ModifySystemRole = modifySystemRole;
            ModifyOrganizationRole = modifyOrganizationRole;

            CanModifyAny = modifyProperties || modifyContractRole || modifySystemRole || modifyOrganizationRole;
        }

        public bool CanModifyAny { get; set; }
        public bool ModifyProperties { get; set; }
        public bool ModifyContractRole { get; set; }
        public bool ModifySystemRole { get; set; }
        public bool ModifyOrganizationRole { get; set; }
    }
}