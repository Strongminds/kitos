using Core.ApplicationServices.Authorization;

namespace Core.ApplicationServices.Model.GDPR
{
    public class DataProcessingRegistrationPermissions
    {
        public ResourcePermissionsResult BasePermissions { get; }
        public ModuleFieldsPermissionsResult FieldPermissions { get; set; }
        
        public DataProcessingRegistrationPermissions(ResourcePermissionsResult basePermissions, ModuleFieldsPermissionsResult fieldPermissions)
        {
            BasePermissions = basePermissions;
            FieldPermissions = fieldPermissions;
        }

    }
}
