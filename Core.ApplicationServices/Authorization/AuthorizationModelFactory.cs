namespace Core.ApplicationServices.Authorization;

public class AuthorizationModelFactory(IOrganizationalUserContext userContext, ISupplierAssociatedFieldsService supplierAssociatedFieldsService) : IAuthorizationModelFactory
{
    public CrudAuthorizationModel CreateCrudAuthorizationModel(IAuthorizationContext authorizationContext)
    {
        return new CrudAuthorizationModel(authorizationContext);
    }

    public FieldAuthorizationModel CreateFieldAuthorizationModel(IAuthorizationContext authorizationContext)
    {
        return new FieldAuthorizationModel(userContext, supplierAssociatedFieldsService, authorizationContext);
    }
}