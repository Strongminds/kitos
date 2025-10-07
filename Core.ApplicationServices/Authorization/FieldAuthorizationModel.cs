using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel(IOrganizationalUserContext activeUserContext, ISupplierAssociatedFieldsService supplierAssociatedFieldsService, IAuthorizationContext authorizationContext) : IAuthorizationModel
{
    public bool AuthorizeUpdate(
        IEntity entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        if (activeUserContext.IsGlobalAdmin()) return true;
        var requestsSupplierFieldChanges = supplierAssociatedFieldsService.RequestsChangesToSupplierAssociatedFields(parameters);
        if (!requestsSupplierFieldChanges) return authorizationContext.AllowModify(entity);

        return false;
    }
}