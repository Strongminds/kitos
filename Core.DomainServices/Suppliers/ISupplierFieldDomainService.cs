using System;
using System.Collections.Generic;

namespace Core.DomainServices.Suppliers
{
    public interface ISupplierFieldDomainService
    {
        bool ContainsOnlySupplierControlledAndSharedFields(IEnumerable<string> properties);
        bool ContainsAnySupplierControlledFields(IEnumerable<string> properties, Guid organizationUuid);
        bool IsSupplierControlled(string key, Guid organizationUuid);
    }
}
