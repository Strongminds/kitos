using System.Collections.Generic;
using System.Linq;
using Core.ApplicationServices.Model;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainServices;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel(IOrganizationalUserContext activeUserContext, ISupplierAssociatedFieldsService supplierAssociatedFieldsService, IAuthorizationContext authorizationContext, IUserRepository userRepository) : IAuthorizationModel
{
    public bool AuthorizeUpdate(
        IEntityOwnedByOrganization entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        //maybe to be safe check if suppliers exist again here to make it safe to use outside orgAuthContext? if none exist, we just go to allowModify(e)
        if (activeUserContext.IsGlobalAdmin()) return true;

        var userHasSupplierApiAccess = UserHasSupplierApiAccess(entity.Organization.Suppliers);
        return userHasSupplierApiAccess
            ? CheckForSupplierApiUser(entity, parameters)
            : CheckForNonSupplierApiUser(entity, parameters);
        /*
        var requestsSupplierFieldChanges = supplierAssociatedFieldsService.RequestsChangesToSupplierAssociatedFields(parameters);
        if (!requestsSupplierFieldChanges) return authorizationContext.AllowModify(entity);

        var userHasSupplierApiAccess = UserHasSupplierApiAccess(entity.Organization.Suppliers);
        if (!userHasSupplierApiAccess) return false;

        var requestsNonSupplierFieldChanges = supplierAssociatedFieldsService.RequestsChangesToNonSupplierAssociatedFields(parameters, entity.Id);
        if (requestsNonSupplierFieldChanges) return false;

        return authorizationContext.AllowModify(entity);
        */
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

    private bool UserHasSupplierApiAccess(IEnumerable<OrganizationSupplier> suppliers)
    {
        foreach (var supplier in suppliers)
        {
            var supplierApiUsers = userRepository.GetUsersInOrganization(supplier.SupplierId)
                .Where(u => u.HasApiAccess ?? false);
            //todo easier check: we have access to orgIds from activeUser - compare those to supplierIds of organization. if user has apiaccess and any of those orgId pairs match, return true
            //extend userctx with userCtx.HasSupplierApiAccess(int[] orgSupplierIds) check
            if (supplierApiUsers.Any(u => u.Id == activeUserContext.UserId)) return true;
        }

        return false;
    }
}