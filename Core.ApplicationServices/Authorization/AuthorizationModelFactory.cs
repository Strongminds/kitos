using System;

namespace Core.ApplicationServices.Authorization;

public class AuthorizationModelFactory : IAuthorizationModelFactory
{
    public CrudAuthorizationModel CreateCrudAuthorizationModel()
    {
        throw new NotImplementedException();
    }

    public FieldAuthorizationModel CreateFieldAuthorizationModel()
    {
        throw new NotImplementedException();
    }
}