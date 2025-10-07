using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization
{
    public interface IAuthorizationModel
    {
        bool AuthorizeUpdate(IEntity entity,
            ISupplierAssociatedEntityUpdateParameters parameters);
    }
}
