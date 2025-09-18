namespace Presentation.Web.Models.API.V2.Internal.Response.User
{
    public class UserCollectionEditPermissionsResponseDTO
    {
        public UserCollectionEditPermissionsResponseDTO(bool edit, bool editProperties, bool editContractRole, bool editSystemRole, bool editOrganizationRole)
        {
            EditProperties = editProperties;
            EditContractRole = editContractRole;
            EditSystemRole = editSystemRole;
            EditOrganizationRole = editOrganizationRole;

            Edit = editProperties || editContractRole || editSystemRole || editOrganizationRole;
        }

        public bool Edit { get; set; }
        public bool EditProperties { get; set; }
        public bool EditContractRole { get; set; }
        public bool EditSystemRole { get; set; }
        public bool EditOrganizationRole { get; set; }
    }
}