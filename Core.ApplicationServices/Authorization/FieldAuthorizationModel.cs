using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel(IOrganizationalUserContext activeUserContext) : IAuthorizationModel
{
    public bool AuthorizeUpdate(
        IEntity entity,
        ISupplierAssociatedEntityUpdateParameters parameters)
    {
        if (activeUserContext.IsGlobalAdmin()) return true;
        return false;
    }
}