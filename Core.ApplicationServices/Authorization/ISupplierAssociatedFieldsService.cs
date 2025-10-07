using System;
using Core.ApplicationServices.Model;

namespace Core.ApplicationServices.Authorization
{
    public interface ISupplierAssociatedFieldsService
    {
        bool RequestsChangesToSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters);
        bool RequestsChangesToNonSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters, Guid entityUuid);
    }
}
