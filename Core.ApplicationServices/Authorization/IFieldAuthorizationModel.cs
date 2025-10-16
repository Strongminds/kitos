using Core.DomainModel;

namespace Core.ApplicationServices.Authorization
{
    public interface IFieldAuthorizationModel
    {
        FieldPermissionsResult GetFieldPermissions(IEntityOwnedByOrganization entity, string key);
    }
}
