using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Abstractions.Types;
using Core.ApplicationServices.GDPR;
using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared.Write;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    private readonly IDataProcessingRegistrationApplicationService _dataProcessingRegistrationApplicationService;
    private const string _hasChangePropertyName = "HasChange";

    public SupplierAssociatedFieldsService(IDataProcessingRegistrationApplicationService dataProcessingRegistrationApplicationService)
    {
        _dataProcessingRegistrationApplicationService = dataProcessingRegistrationApplicationService;
    }
    public bool RequestsChangesToSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters)
    {
        if (parameters.GetType() != typeof(DataProcessingRegistrationModificationParameters)) return false;
        //todo add remaining target fields when miol responds. Then also update the inverse check below to ignore those fields
        var dprParams = (DataProcessingRegistrationModificationParameters)parameters;
       
        var oversight = dprParams.Oversight;
        return oversight.HasValue && oversight.Value.IsOversightCompleted.HasChange;
    }

    public bool RequestsChangesToNonSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters, Guid dataProcessingUuid)
    {
        if (parameters.GetType() != typeof(DataProcessingRegistrationModificationParameters)) return false;
        var dprParams = (DataProcessingRegistrationModificationParameters)parameters;

        var nameHasChange = dprParams.Name.HasChange;
        var generalHasChange = dprParams.General.HasValue && AnyOptionalValueChangeFieldHasChange(dprParams.General.Value);
        var oversightHasNonSupplierAssociatedChange = OversightHasNonSupplierAssociatedChange(dprParams.Oversight);
        var rolesHasChange = dprParams.Roles.HasValue && AnyOptionalValueChangeFieldHasChange(dprParams.Roles.Value);
        var systemUsageUuidsHasChange = SystemUsageUuidsHasChange(dprParams.SystemUsageUuids, dataProcessingUuid);
        var externalReferencesHasChange = ExternalReferencesHasChange(dprParams.ExternalReferences, dataProcessingUuid);
        return nameHasChange || generalHasChange || oversightHasNonSupplierAssociatedChange || rolesHasChange || systemUsageUuidsHasChange || externalReferencesHasChange;
    }

    private bool ExternalReferencesHasChange(Maybe<IEnumerable<UpdatedExternalReferenceProperties>> updatedReferencesMaybe, Guid dprUuid)
    {
        var dataProcessingRegistrationResult = _dataProcessingRegistrationApplicationService.GetByUuid(dprUuid);
        return dataProcessingRegistrationResult.Match(dataProcessingRegistration =>
            {
                var existingReferences = dataProcessingRegistration.ExternalReferences;
                if (updatedReferencesMaybe.IsNone && IsNullOrEmpty(existingReferences)) return false;
                if (updatedReferencesMaybe.HasValue && existingReferences != null)
                {
                    var updatedReferences = updatedReferencesMaybe.Value.ToList();
                    return existingReferences.Count != updatedReferences.Count() || updatedReferences.Any(u =>
                        existingReferences.FirstOrDefault(e => e.Uuid == u.Uuid) == null);
                }
                return true;

            }, _ => false
        );
    }

    private bool OversightHasNonSupplierAssociatedChange(
        Maybe<UpdatedDataProcessingRegistrationOversightDataParameters> parameters)
    {
        if (parameters.IsNone) return false;
        var value = parameters.Value;
        return value.OversightOptionUuids.HasChange ||
               value.OversightOptionsRemark.HasChange ||
               value.OversightInterval.HasChange ||
               value.OversightIntervalRemark.HasChange ||
               value.OversightCompletedRemark.HasChange ||
               value.OversightScheduledInspectionDate.HasChange ||
               value.OversightDates.HasChange;
    }

    private bool SystemUsageUuidsHasChange(Maybe<IEnumerable<Guid>> updatedSystemUsageUuids, Guid dataProcessingUuid)
    {
        var dataProcessingRegistrationResult = _dataProcessingRegistrationApplicationService.GetByUuid(dataProcessingUuid);
        return dataProcessingRegistrationResult.Match(dataProcessingRegistration =>
        {
            var existingSystemUsages = dataProcessingRegistration.SystemUsages;
            if (updatedSystemUsageUuids.IsNone && IsNullOrEmpty(existingSystemUsages)) return false;
            if (updatedSystemUsageUuids.HasValue && dataProcessingRegistration.SystemUsages != null)
            {
                var existingSystemUsageUuids = dataProcessingRegistration.SystemUsages.Select(su => su.Uuid).ToHashSet();
                var updatedSystemUsageUuidsHashSet = updatedSystemUsageUuids.Value.ToHashSet();
                return !existingSystemUsageUuids.SetEquals(updatedSystemUsageUuidsHashSet);
            }
            return true;
            }, _ => false);
    }

    private bool AnyOptionalValueChangeFieldHasChange(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj)); 

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var optionalValueChange = property.GetValue(obj);
            var hasChange = optionalValueChange?.GetType().GetProperty(_hasChangePropertyName)?.GetValue(optionalValueChange);
            if (hasChange != null && (bool)hasChange) return true;
        }

        foreach (var field in fields)
        {
            var optionalValueChange = field.GetValue(obj);
            var hasChange = optionalValueChange?.GetType().GetProperty(_hasChangePropertyName)?.GetValue(optionalValueChange);
            if (hasChange != null && (bool)hasChange) return true;
        }

        return false;
    }
    private bool IsNullOrEmpty<T>(ICollection<T> collection)
    {
        return collection == null || !collection.Any();
    }
}

