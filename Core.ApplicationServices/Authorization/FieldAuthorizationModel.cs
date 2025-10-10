using System.Linq;
using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel(IOrganizationalUserContext activeUserContext, ISupplierAssociatedFieldsService supplierAssociatedFieldsService, IAuthorizationContext authorizationContext) : IAuthorizationModel
{
    public bool AuthorizeUpdate(
        IEntityOwnedByOrganization entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        if (entity == null || parameters == null) return false;

        if (activeUserContext.IsGlobalAdmin()) return true;
        var organizationHasSuppliers = entity.Organization?.HasSuppliers() ?? false;
        if (!organizationHasSuppliers) return authorizationContext.AllowModify(entity);

        var supplierIds = entity.Organization.Suppliers.Select(x => x.SupplierId);
        var userHasSupplierApiAccess = activeUserContext.IsSupplierApiUserForOrganizationWithSuppliers(supplierIds);
        return userHasSupplierApiAccess
            ? CheckForSupplierApiUser(entity, parameters)
            : CheckForNonSupplierApiUser(entity, parameters);
    }

    private bool CheckForSupplierApiUser(IEntityOwnedByOrganization entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        var requestsNonSupplierFieldChanges = supplierAssociatedFieldsService.RequestsChangesToNonSupplierAssociatedFields(parameters, entity.Id);
        if (requestsNonSupplierFieldChanges) return authorizationContext.AllowModify(entity);
        return true;
    }

    private bool CheckForNonSupplierApiUser(IEntityOwnedByOrganization entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        var requestsSupplierFieldChanges = supplierAssociatedFieldsService.RequestsChangesToSupplierAssociatedFields(parameters);
        if (requestsSupplierFieldChanges) return false;
        return authorizationContext.AllowModify(entity);

    }
}