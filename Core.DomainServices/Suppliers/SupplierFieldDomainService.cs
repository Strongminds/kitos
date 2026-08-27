using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.SupplierAssociatedFields;
using Core.DomainServices.Repositories.Organization;
using System;

namespace Core.DomainServices.Suppliers
{
    public class SupplierFieldDomainService : ISupplierFieldDomainService
    {
        private readonly ISet<SupplierAssociatedFieldConfiguration> _defaultFieldConfigurations;
        private readonly IOrganizationRepository _organizationRepository;

        public SupplierFieldDomainService(IOrganizationRepository organizationRepository)
        {
            _defaultFieldConfigurations = SupplierAssociatedFields.DefaultConfiguration;
            _organizationRepository = organizationRepository;
        }
                
        public bool ContainsOnlySupplierControlledAndSharedFields(IEnumerable<string> properties, Guid organizationUuid)
        {
            return _organizationRepository.GetSupplierAssociatedFieldConfigurations(organizationUuid)
                .Match(
                    (organizationalConfigurations) =>
                       {
                        foreach (var property in properties)
                        {
                            var organizationalField = organizationalConfigurations.FirstOrDefault(x => x.FieldKey == property);
                            if (organizationalField != null && !(organizationalField.HasSupplierControlState || organizationalField.HasSharedControlState))
                                return false;

                            if (organizationalField == null && !HasSupplierOrSharedAccessInDefaultConfigurations(property))
                                return false;
                        }
                        return true;
                    },
                    () => properties.All(HasSupplierOrSharedAccessInDefaultConfigurations)
            );
        }

        public bool ContainsAnySupplierControlledFields(IEnumerable<string> properties, Guid organizationUuid)
        {
            return _organizationRepository.GetSupplierAssociatedFieldConfigurations(organizationUuid).Match(
                (organizationalConfigurations) =>
                {
                    foreach (var property in properties)
                    {
                        var organizationalField = organizationalConfigurations.FirstOrDefault(x => x.FieldKey == property);
                        if (organizationalField != null && organizationalField.HasSupplierControlState)
                            return true;

                        if (organizationalField == null && IsSupplierControlledInDefaultConfigurations(property))
                            return true;
                    }

                    return false;
                },
                () => properties.Any(IsSupplierControlledInDefaultConfigurations)
            );     
        }

        public bool IsSupplierControlled(string key, Guid organizationUuid)
        {
            return _organizationRepository.GetSupplierAssociatedFieldConfigurations(organizationUuid).Match(
                (organizationalConfigurations) =>
                {
                    var organizationalField = organizationalConfigurations.FirstOrDefault(x => x.FieldKey == key);

                    return organizationalField == null
                        ? IsSupplierControlledInDefaultConfigurations(key)
                        : organizationalField.HasSupplierControlState;
                },
                () => IsSupplierControlledInDefaultConfigurations(key)
            );           
        }

        private SupplierAssociatedFieldConfiguration? TryGetFromDefaultConfiguration(string key)
        {
            return _defaultFieldConfigurations.FirstOrDefault(x => x.FieldKey == key);
        }

        private bool HasSupplierOrSharedAccessInDefaultConfigurations(string key)
        {
            var configuration = TryGetFromDefaultConfiguration(key);
            if (configuration == null) return false;
            return configuration.HasSupplierControlState || configuration.HasSharedControlState;
        }

        private bool IsSupplierControlledInDefaultConfigurations(string key)
        {
            var configuration = TryGetFromDefaultConfiguration(key);
            if (configuration == null) return false;
            return configuration.HasSupplierControlState;
        }
    }
}
