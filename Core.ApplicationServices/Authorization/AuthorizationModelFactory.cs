using System;

namespace Core.ApplicationServices.Authorization;

public class AuthorizationModelFactory : IAuthorizationModelFactory
{
    public CrudAuthorizationModel CreateCrudAuthorizationModel()
    {
        return new CrudAuthorizationModel();
    }

    public FieldAuthorizationModel CreateFieldAuthorizationModel()
    {
        return new FieldAuthorizationModel();
    }
}