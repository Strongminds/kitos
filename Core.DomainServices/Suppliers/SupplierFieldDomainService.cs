using Core.Abstractions.Helpers;
using Core.DomainModel.GDPR;
using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.SupplierAssociatedFields;
using Core.DomainServices.Repositories.Organization;

namespace Core.DomainServices.Suppliers
{
    public class SupplierFieldDomainService : ISupplierFieldDomainService
    {
        private readonly ISet<string> _supplierOnlyControlledFieldKeys;
        private readonly ISet<string> _sharedFieldKeys;
        private readonly IEnumerable<SupplierAssociatedFieldConfiguration> _defaultFieldConfigurations;
        private readonly IOrganizationRepository _organizationRepository;

        public SupplierFieldDomainService(IOrganizationRepository organizationRepository)
        {
            _supplierOnlyControlledFieldKeys = new HashSet<string>
            {
                ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted),
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate),
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark),
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink),
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightOptionId),
                ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.ContainsAITechnology),
                ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.SystemUsageCriticalityLevel),
                ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.preriskAssessment),

            };
            _sharedFieldKeys = new HashSet<string>
            {
                ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLinkName),
                ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.riskAssessment)
            };

            _defaultFieldConfigurations = new List<SupplierAssociatedFieldConfiguration>
            {
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightOptionId), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.ContainsAITechnology), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.SystemUsageCriticalityLevel), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.preriskAssessment), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLinkName), ControlState = SupplierAssociatedFieldControlState.SHARED },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.riskAssessment), ControlState = SupplierAssociatedFieldControlState.SHARED },
            };

            _organizationRepository = organizationRepository;
        }

        public IEnumerable<SupplierAssociatedFieldConfiguration> GetDefaultFieldConfigurations => _defaultFieldConfigurations;

        public bool ContainsOnlySupplierControlledField(IEnumerable<string> properties)
        {
            return properties.All(x => _supplierOnlyControlledFieldKeys.Contains(x) || _sharedFieldKeys.Contains(x));
        }

        public bool ContainsAnySupplierControlledFields(IEnumerable<string> properties)
        {
            return properties.Any(_supplierOnlyControlledFieldKeys.Contains);
        }

        public bool IsSupplierControlled(string key)
        {
            return _supplierOnlyControlledFieldKeys.Contains(key);
        }
    }
}
