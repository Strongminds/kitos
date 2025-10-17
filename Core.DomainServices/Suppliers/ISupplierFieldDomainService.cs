using System.Collections.Generic;

namespace Core.DomainServices.Suppliers
{
    public interface ISupplierFieldDomainService
    {
        bool OnlySupplierFieldChanges(IEnumerable<string> properties);
        bool AnySupplierFieldChanges(IEnumerable<string> properties);
        bool IsSupplierControlled(string key);
    }
}
