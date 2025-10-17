using Core.Abstractions.Types;
using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Abstractions.Helpers;
using Core.DomainServices.Suppliers;
using Core.Abstractions.Extensions;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    private const string HasChangePropertyName = "HasChange";
    private const string SystemUsageUuidsPropertyName = nameof(DataProcessingRegistrationModificationParameters.SystemUsageUuids);
    private const string ExternalReferencesPropertyName = nameof(DataProcessingRegistrationModificationParameters.ExternalReferences);

    private readonly Dictionary<string, string> _dataProcessingParameterToSupplierFieldMap;

    private readonly ISupplierFieldDomainService _supplierFieldDomainService;

    public SupplierAssociatedFieldsService(ISupplierFieldDomainService supplierFieldDomainService)
    {
        _supplierFieldDomainService = supplierFieldDomainService;
        _dataProcessingParameterToSupplierFieldMap = new Dictionary<string, string>
        {
            { nameof(UpdatedDataProcessingRegistrationOversightDataParameters.IsOversightCompleted), ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted) },
            { nameof(DataProcessingRegistrationOversightDate.OversightDate), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate) },
            { nameof(DataProcessingRegistrationOversightDate.OversightRemark), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark)},
            { nameof(DataProcessingRegistrationOversightDate.OversightReportLink), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink) },
        };
    }

    public bool RequestsChangesToSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters)
    {
        return parameters switch
        {
            DataProcessingRegistrationModificationParameters dprParameters => CheckSupplierChangesToDprParams(
                dprParameters),
            UpdatedDataProcessingRegistrationOversightDateParameters oversightDateParameters =>
                CheckSupplierChangesToDprOversightDateParams(oversightDateParameters),
            _ => false
        };
    }

    private bool CheckSupplierChangesToDprParams(DataProcessingRegistrationModificationParameters dprParams)
    {
        var oversight = dprParams.Oversight;
        if (oversight.IsNone)
            return false;

        return _supplierFieldDomainService.OnlySupplierFieldChanges(GetFieldDomainKeys(GetChangedProperties(oversight.Value)));
    }

    private IEnumerable<string> GetFieldDomainKeys(IEnumerable<string> properties)
    {
        foreach (var key in properties)
        {
            if (_dataProcessingParameterToSupplierFieldMap.TryGetValue(key, out var mappedValue))
            {
                yield return mappedValue;
            }
        }
    }

    private bool CheckSupplierChangesToDprOversightDateParams(UpdatedDataProcessingRegistrationOversightDateParameters parameters)
    {
        return _supplierFieldDomainService.OnlySupplierFieldChanges(
            GetFieldDomainKeys(GetChangedProperties(parameters)));
    }

    public bool RequestsChangesToNonSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters, IEntity entity)
    {
        return parameters switch
        {
            DataProcessingRegistrationModificationParameters dprParameters => CheckNonSupplierChangesToDprParams(
                dprParameters, entity),
            UpdatedDataProcessingRegistrationOversightDateParameters oversightDateParameters =>
                CheckNonSupplierChangesToDprOversightDateParams(oversightDateParameters),
            _ => false
        };
    }
    public bool RequestsDeleteToEntity<TEntity>(TEntity entity)
    {
        return entity switch
        {
            DataProcessingRegistrationOversightDate => true,
            _ => false
        };
    }

    public bool RequestsChangesToSupplierAssociatedFieldsInEnumerable(IEnumerable<ISupplierAssociatedEntityUpdateParameters> parametersEnumerable)
    {
        var results = parametersEnumerable.Select(RequestsChangesToSupplierAssociatedFields);
        return results.Any(r => r);
    }

    private static bool CheckNonSupplierChangesToDprOversightDateParams(UpdatedDataProcessingRegistrationOversightDateParameters parameters)
    {
        return false;
    }

    public bool IsFieldSupplierControlled(string key)
    {
        return _supplierFieldDomainService.IsSupplierControlled(key);
    }

    private bool CheckNonSupplierChangesToDprParams(DataProcessingRegistrationModificationParameters dprParams, IEntity entity)
    {
        var changedProperties = GetChangedProperties(dprParams).ToList();
        if(dprParams.General.HasValue)
            changedProperties.AddRange(GetChangedProperties(dprParams.General.Value));
        if(dprParams.Roles.HasValue)
            changedProperties.AddRange(GetChangedProperties(dprParams.Roles.Value));
        if(dprParams.Oversight.HasValue)
            changedProperties.AddRange(GetChangedProperties(dprParams.Oversight.Value));
        if(SystemUsageUuidsHasChange(dprParams.SystemUsageUuids, entity))
            changedProperties.Add(SystemUsageUuidsPropertyName);
        if(ExternalReferencesHasChange(dprParams.ExternalReferences, entity))
            changedProperties.Add(ExternalReferencesPropertyName);

        return _supplierFieldDomainService.AnySupplierFieldChanges(GetFieldDomainKeys(changedProperties));
    }

    private static bool ExternalReferencesHasChange(Maybe<IEnumerable<UpdatedExternalReferenceProperties>> updatedReferencesMaybe, IEntity entity)
    {
        if (entity is not DataProcessingRegistration dpr) return false;
        
        var existingReferences = dpr.ExternalReferences;
        if (updatedReferencesMaybe.IsNone && existingReferences.IsNullOrEmpty()) return false;
        if (updatedReferencesMaybe.HasValue && existingReferences != null)
        {
            var updatedReferences = updatedReferencesMaybe.Value.ToList();
            return existingReferences.Count != updatedReferences.Count() || updatedReferences.Any(u =>
                existingReferences.FirstOrDefault(e => e.Uuid == u.Uuid) == null);
        }
        return true;

    }
    
    private static bool SystemUsageUuidsHasChange(Maybe<IEnumerable<Guid>> updatedSystemUsageUuids, IEntity entity)
    {
        if (entity is not DataProcessingRegistration dpr) return false;

        var existingSystemUsages = dpr.SystemUsages;
        if (updatedSystemUsageUuids.IsNone && existingSystemUsages.IsNullOrEmpty()) return false;
        if (updatedSystemUsageUuids.HasValue && existingSystemUsages != null)
        {
            var existingSystemUsageUuids = existingSystemUsages.Select(su => su.Uuid).ToHashSet();
            var updatedSystemUsageUuidsHashSet = updatedSystemUsageUuids.Value.ToHashSet();
            return !existingSystemUsageUuids.SetEquals(updatedSystemUsageUuidsHashSet);
        }
        return true;
    }

    private static IEnumerable<string> GetChangedProperties(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            if (value == null) continue;

            var hasChangeProp = value.GetType().GetProperty(HasChangePropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (hasChangeProp != null)
            {
                var hasChangeValue = hasChangeProp.GetValue(value);
                if (hasChangeValue is bool b && b)
                {
                    yield return prop.Name;
                }
            }
        }
    }
}

