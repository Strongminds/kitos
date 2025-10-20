using Core.Abstractions.Helpers;
using Core.DomainModel.GDPR;
using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.ItSystemUsage;

namespace Core.DomainServices.Suppliers
{
    public class SupplierFieldDomainService : ISupplierFieldDomainService
    {
        private readonly ISet<string> _supplierOnlyControlledFieldKeys;
        private readonly ISet<string> _sharedFieldKeys;

        public SupplierFieldDomainService()
        {
            _supplierOnlyControlledFieldKeys = new HashSet<string>
            {
                ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted),
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate),
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark),
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink),
                ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.ContainsAITechnology)

            };
            _sharedFieldKeys = new HashSet<string>
            {
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLinkName)
            };
        }

        public bool OnlySupplierFieldChanges(IEnumerable<string> properties)
        {
            return properties.All(x => _supplierOnlyControlledFieldKeys.Contains(x) || _sharedFieldKeys.Contains(x));
        }

        public bool AnySupplierFieldChanges(IEnumerable<string> properties)
        {
            return properties.Any(_supplierOnlyControlledFieldKeys.Contains);
        }

        public bool IsSupplierControlled(string key)
        {
            return _supplierOnlyControlledFieldKeys.Contains(key);
        }
    }
}
