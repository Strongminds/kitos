namespace Presentation.Web.Models.API.V2.Response.Shared
{
    public class CombinedPermissionsResponseDTO : ResourcePermissionsResponseDTO
    {
        public ModuleFieldPermissionsResponseDTO FieldPermissions { get; set; }
    }
}