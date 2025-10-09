﻿using System.Collections.Generic;
using Core.ApplicationServices.Model;

namespace Core.ApplicationServices.Authorization
{
    public interface ISupplierAssociatedFieldsService
    {
        bool RequestsChangesToSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters);
        bool RequestsChangesToNonSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters, int entityId);
        bool RequestsChangesToSupplierAssociatedFieldsInEnumerable(IEnumerable<ISupplierAssociatedEntityUpdateParameters> parametersEnumerable);
    }
}
