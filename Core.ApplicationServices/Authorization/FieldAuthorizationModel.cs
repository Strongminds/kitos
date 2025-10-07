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
        IEntity entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        if (activeUserContext.IsGlobalAdmin()) return true;
        var requestsSupplierFieldChanges = supplierAssociatedFieldsService.RequestsChangesToSupplierAssociatedFields(parameters);
        if (!requestsSupplierFieldChanges) return authorizationContext.AllowModify(entity);

       // var userHasSupplierApiAccess = UserHasSupplierApiAccess(entity.Organization?.Suppliers);
       // if (!userHasSupplierApiAccess) return false;


        return false;
    }

    private bool UserHasSupplierApiAccess(IEnumerable<OrganizationSupplier> suppliers)
    {
        foreach (var supplier in suppliers)
        {
            var supplierUsers = userRepository.GetUsersInOrganization(supplier.SupplierId)
                .Where(u => u.HasApiAccess ?? false);
            if (supplierUsers.Any(u => u.Id == activeUserContext.UserId)) return true;
        }

        return false;
    }
}