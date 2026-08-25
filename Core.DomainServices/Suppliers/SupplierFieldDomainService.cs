using Core.Abstractions.Helpers;
using Core.DomainModel.GDPR;
using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.SupplierAssociatedFields;
using Core.DomainServices.Repositories.Organization;
using System;

namespace Core.DomainServices.Suppliers
{
    public class SupplierFieldDomainService : ISupplierFieldDomainService
    {
        private readonly IEnumerable<SupplierAssociatedFieldConfiguration> _defaultFieldConfigurations;
        private readonly IOrganizationRepository _organizationRepository;

        public SupplierFieldDomainService(IOrganizationRepository organizationRepository)
        {
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
            return properties.All(x => IsSupplierControlledInDefaultConfigurations(x) || HasSharedAccess(x));
        }

        public bool ContainsAnySupplierControlledFields(IEnumerable<string> properties)
        {
            return properties.Any(IsSupplierControlledInDefaultConfigurations);
        }

        private bool HasSharedAccess(string key)
        {
            var configuration = _defaultFieldConfigurations.FirstOrDefault(x => x.FieldKey == key);
            if (configuration == null) return false;
            return configuration.ControlState == SupplierAssociatedFieldControlState.SHARED;
        }

        private bool IsSupplierControlledInDefaultConfigurations(string key)
        {
            var configuration = _defaultFieldConfigurations.FirstOrDefault(x => x.FieldKey == key);
            if (configuration == null) return false;
            return HasSupplierControlState(configuration);
        }

        public bool IsSupplierControlled(string key, Guid organizationUuid)
        {
            var organizationalConfigurationsMaybe = _organizationRepository.GetSupplierAssociatedFieldConfigurations(organizationUuid);
            if (organizationalConfigurationsMaybe.IsNone) return IsSupplierControlledInDefaultConfigurations(key);
            var organizationalConfigurations = organizationalConfigurationsMaybe.Value;
            var organizationalField = organizationalConfigurations.FirstOrDefault(x => x.FieldKey == key);

            return organizationalField == null
                ? IsSupplierControlledInDefaultConfigurations(key)
                : HasSupplierControlState(organizationalField);
        }

        private bool HasSupplierControlState(SupplierAssociatedFieldConfiguration field)
        {
            return field.ControlState == SupplierAssociatedFieldControlState.SUPPLIER;
        }
    }
}
