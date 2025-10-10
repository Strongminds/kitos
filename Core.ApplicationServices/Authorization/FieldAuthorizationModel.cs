using System.Collections.Generic;
using System.Linq;
using Core.ApplicationServices.Model;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainServices;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel(IOrganizationalUserContext activeUserContext, ISupplierAssociatedFieldsService supplierAssociatedFieldsService, IAuthorizationContext authorizationContext) : IAuthorizationModel
{
    public bool AuthorizeUpdate(
        IEntityOwnedByOrganization entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        // to be safe check if suppliers exist again here to make it safe to use outside orgAuthContext? if none exist, we just go to allowModify(e)
        if (activeUserContext.IsGlobalAdmin()) return true;

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