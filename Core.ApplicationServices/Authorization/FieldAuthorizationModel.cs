using System;
using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel : IAuthorizationModel
{
    public bool AuthorizeUpdate(
        IEntity entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        throw new NotImplementedException();
    }
}