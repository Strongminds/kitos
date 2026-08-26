using Core.ApplicationServices.Model;
using Core.DomainModel;
using System;

namespace Core.ApplicationServices.Authorization
{
    public interface IAuthorizationModel
    {
        bool AuthorizeUpdate(IEntityOwnedByOrganization entity,
            ISupplierAssociatedEntityUpdateParameters parameters, Guid organizationUuid);
        bool AuthorizeChildEntityDelete<TChild>(IEntityOwnedByOrganization parent, TChild child) where TChild : class;
    }
}
