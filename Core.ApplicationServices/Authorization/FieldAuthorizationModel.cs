using System.Linq;
using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel : IAuthorizationModel, IFieldAuthorizationModel
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

    public bool AuthorizeChildEntityDelete<TChild>(IEntityOwnedByOrganization parent, TChild child) where TChild : class
    {
        if (parent == null || child == null) return false;

        if (_activeUserContext.IsGlobalAdmin()) return true;
        var entityOrganization = parent.Organization;
        var organizationHasSuppliers = entityOrganization?.HasSuppliers() ?? false;
        if (!organizationHasSuppliers) return _authorizationContext.AllowModify(parent);

        var supplierIds = entityOrganization.Suppliers.ToHashSet().Select(x => x.SupplierId);
        var userHasSupplierApiAccess = _activeUserContext.IsSupplierApiUserForOrganizationWithSuppliers(supplierIds);
        var requestsDeleteForSupplierControlledEntity = _supplierAssociatedFieldsService.RequestsDeleteToEntity(child);
        return userHasSupplierApiAccess
            ? requestsDeleteForSupplierControlledEntity
            : requestsDeleteForSupplierControlledEntity == false;
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

    public FieldPermissionsResult GetFieldPermissions(IEntityOwnedByOrganization entity, string key)
    {
        if (_activeUserContext.IsGlobalAdmin()) 
            return new FieldPermissionsResult{ Enabled = true, Key = key};

        var entityOrganization = entity.Organization;
        var organizationHasSuppliers = entityOrganization?.HasSuppliers() ?? false;
        if (!organizationHasSuppliers)
            return new FieldPermissionsResult { Enabled = true, Key = key };

        return new FieldPermissionsResult
                { Enabled = _supplierAssociatedFieldsService.IsFieldSupplierControlled(key) == false, Key = key };
    }
}
