namespace Core.ApplicationServices.Authorization;

public class AuthorizationModelFactory(IAuthorizationContext authorizationContext, IOrganizationalUserContext userContext) : IAuthorizationModelFactory
{
    public CrudAuthorizationModel CreateCrudAuthorizationModel()
    {
        return new CrudAuthorizationModel(authorizationContext);
    }

    public FieldAuthorizationModel CreateFieldAuthorizationModel()
    {
        return new FieldAuthorizationModel(userContext);
    }
}