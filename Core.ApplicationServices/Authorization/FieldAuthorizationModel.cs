using System.Linq;
using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel : IAuthorizationModel
{
    private readonly IOrganizationalUserContext _activeUserContext;
    private readonly ISupplierAssociatedFieldsService _supplierAssociatedFieldsService;
    private readonly IAuthorizationContext _authorizationContext;

    public FieldAuthorizationModel(IOrganizationalUserContext activeUserContext, ISupplierAssociatedFieldsService supplierAssociatedFieldsService, IAuthorizationContext authorizationContext)
    {
        _activeUserContext = activeUserContext;
        _supplierAssociatedFieldsService = supplierAssociatedFieldsService;
        _authorizationContext = authorizationContext;
    }

    public bool AuthorizeUpdate(
        IEntityOwnedByOrganization entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        if (entity == null || parameters == null) return false;

        if (_activeUserContext.IsGlobalAdmin()) return true;
       var entityOrganization = entity.Organization;
        var organizationHasSuppliers = entityOrganization?.HasSuppliers() ?? false;
        if (!organizationHasSuppliers) return _authorizationContext.AllowModify(entity);

        var supplierIds = entityOrganization.Suppliers.ToHashSet().Select(x => x.SupplierId);
        var userHasSupplierApiAccess = _activeUserContext.IsSupplierApiUserForOrganizationWithSuppliers(supplierIds);
         return userHasSupplierApiAccess
            ? CheckForSupplierApiUser(entity, parameters)
            : CheckForNonSupplierApiUser(entity, parameters);
    }
    
    private bool CheckForSupplierApiUser(IEntityOwnedByOrganization entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        var requestsNonSupplierFieldChanges = _supplierAssociatedFieldsService.RequestsChangesToNonSupplierAssociatedFields(parameters, entity);
        if (requestsNonSupplierFieldChanges) return _authorizationContext.AllowModify(entity);
        return true;
    }

    private bool CheckForNonSupplierApiUser(IEntityOwnedByOrganization entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        var requestsSupplierFieldChanges = _supplierAssociatedFieldsService.RequestsChangesToSupplierAssociatedFields(parameters);
        if (requestsSupplierFieldChanges) return false;
        return _authorizationContext.AllowModify(entity);

    }
}
