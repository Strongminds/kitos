using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Abstractions.Types;
using Core.ApplicationServices.GDPR;
using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    private const string _hasChangePropertyName = "HasChange";

    public SupplierAssociatedFieldsService()
    {
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

    private bool CheckSupplierChangesToDprOversightDateParams(UpdatedDataProcessingRegistrationOversightDateParameters parameters)
    {
        return parameters.CompletedAt.HasChange || parameters.Remark.HasChange || parameters.OversightReportLink.HasChange;
    }

    private bool CheckSupplierChangesToDprParams(DataProcessingRegistrationModificationParameters dprParams)
    {
        var oversight = dprParams.Oversight;
        return oversight.HasValue && oversight.Value.IsOversightCompleted.HasChange;
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

    public bool RequestsChangesToSupplierAssociatedFieldsInEnumerable(IEnumerable<ISupplierAssociatedEntityUpdateParameters> parametersEnumerable)
    {
        var results = parametersEnumerable.Select(RequestsChangesToSupplierAssociatedFields);
        return results.Any(r => r);
    }

    private bool CheckNonSupplierChangesToDprOversightDateParams(UpdatedDataProcessingRegistrationOversightDateParameters parameters)
    {
        return false;
    }

    private bool CheckNonSupplierChangesToDprParams(DataProcessingRegistrationModificationParameters dprParams, IEntity entity)
    {
        var nameHasChange = dprParams.Name.HasChange;
        var generalHasChange = dprParams.General.HasValue && AnyOptionalValueChangeFieldHasChange(dprParams.General.Value);
        var oversightHasNonSupplierAssociatedChange = OversightHasNonSupplierAssociatedChange(dprParams.Oversight);
        var rolesHasChange = dprParams.Roles.HasValue && AnyOptionalValueChangeFieldHasChange(dprParams.Roles.Value);
        var systemUsageUuidsHasChange = SystemUsageUuidsHasChange(dprParams.SystemUsageUuids, entity);
        var externalReferencesHasChange = ExternalReferencesHasChange(dprParams.ExternalReferences, entity);
        return nameHasChange || generalHasChange || oversightHasNonSupplierAssociatedChange || rolesHasChange || systemUsageUuidsHasChange || externalReferencesHasChange;
    }

    private bool ExternalReferencesHasChange(Maybe<IEnumerable<UpdatedExternalReferenceProperties>> updatedReferencesMaybe, IEntity entity)
    {
        if (entity is not DataProcessingRegistration dpr) return false;
        
        var existingReferences = dpr.ExternalReferences;
        if (updatedReferencesMaybe.IsNone && IsNullOrEmpty(existingReferences)) return false;
        if (updatedReferencesMaybe.HasValue && existingReferences != null)
        {
            var updatedReferences = updatedReferencesMaybe.Value.ToList();
            return existingReferences.Count != updatedReferences.Count() || updatedReferences.Any(u =>
                existingReferences.FirstOrDefault(e => e.Uuid == u.Uuid) == null);
        }
        return true;

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
               value.OversightScheduledInspectionDate.HasChange;
    }

    private bool SystemUsageUuidsHasChange(Maybe<IEnumerable<Guid>> updatedSystemUsageUuids, IEntity entity)
    {
        if (entity is not DataProcessingRegistration dpr) return false;

        var existingSystemUsages = dpr.SystemUsages;
        if (updatedSystemUsageUuids.IsNone && IsNullOrEmpty(existingSystemUsages)) return false;
        if (updatedSystemUsageUuids.HasValue && existingSystemUsages != null)
        {
            var existingSystemUsageUuids = existingSystemUsages.Select(su => su.Uuid).ToHashSet();
            var updatedSystemUsageUuidsHashSet = updatedSystemUsageUuids.Value.ToHashSet();
            return !existingSystemUsageUuids.SetEquals(updatedSystemUsageUuidsHashSet);
        }
        return true;
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

