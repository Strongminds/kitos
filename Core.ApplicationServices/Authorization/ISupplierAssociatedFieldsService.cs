using System.Collections.Generic;
using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization
{
    public interface ISupplierAssociatedFieldsService
    {
        bool RequestsChangesToSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters);
        bool RequestsChangesToNonSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters, IEntity entity);
        bool RequestsChangesToSupplierAssociatedFieldsInEnumerable(IEnumerable<ISupplierAssociatedEntityUpdateParameters> parametersEnumerable);
        bool RequestsDeleteToEntity<TEntity>(TEntity entity);
    }
}
