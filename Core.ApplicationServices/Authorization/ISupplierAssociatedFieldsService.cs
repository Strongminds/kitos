using System.Collections.Generic;
using Core.ApplicationServices.Model;
using Core.DomainModel;

namespace Core.ApplicationServices.Authorization
{
    public interface ISupplierAssociatedFieldsService
    {
        bool HasAnySupplierChanges(ISupplierAssociatedEntityUpdateParameters parameters, IEntity entity);
        bool HasOnlySupplierChanges(ISupplierAssociatedEntityUpdateParameters parameters, IEntity entity);
        bool RequestsChangesToSupplierAssociatedFieldsInEnumerable(IEnumerable<ISupplierAssociatedEntityUpdateParameters> parametersEnumerable, IEntity entity);
        bool RequestsDeleteToEntity<TEntity>(TEntity entity);
        bool IsFieldSupplierControlled(string key);
        IEnumerable<string> MapParameterKeysToDomainKeys(IEnumerable<string> properties);
    }
}
