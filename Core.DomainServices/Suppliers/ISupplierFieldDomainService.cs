using System.Collections.Generic;

namespace Core.DomainServices.Suppliers
{
    public interface ISupplierFieldDomainService
    {
        bool ContainsOnlySupplierControlledField(IEnumerable<string> properties);
        bool ContainsAnySupplierControlledFields(IEnumerable<string> properties);
        bool IsSupplierControlled(string key);
    }
}
