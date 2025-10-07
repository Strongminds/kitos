using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization;

public class CrudAuthorizationModel(IAuthorizationContext authorizationContext) : IAuthorizationModel
{
    public bool AuthorizeUpdate(IEntity entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        return authorizationContext.AllowModify(entity);
    }
}