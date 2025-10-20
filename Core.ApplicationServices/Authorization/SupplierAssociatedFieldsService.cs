using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared.Write;
using Core.ApplicationServices.Model.SystemUsage.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    private const string HasChangePropertyName = "HasChange";
    private readonly ISet<string> _usageGeneralSupplierAssociatedProperties = new HashSet<string>
    {
        "ContainsAITechnology"
    };
    private readonly ISet<string> _usageGdprSupplierAssociatedProperties = new HashSet<string>
    {
        "GdprCriticality",
        "RiskAssessmentResult"
    };

    private readonly ISet<string> _dprOversightSupplierAssociatedProperties = new HashSet<string>()
    {
        "IsOversightCompleted",
        "OversightDates"
    };

    public bool RequestsChangesToSupplierAssociatedFields(ISupplierAssociatedEntityUpdateParameters parameters)
    {
        return parameters switch
        {
            DataProcessingRegistrationModificationParameters dprParameters => CheckSupplierChangesToDprParams(
                dprParameters),
            UpdatedDataProcessingRegistrationOversightDateParameters oversightDateParameters =>
                CheckSupplierChangesToDprOversightDateParams(oversightDateParameters),
            SystemUsageUpdateParameters usageParameters => CheckSupplierChangesToUsageParameters(usageParameters),
            _ => false
        };
    }

    private bool CheckSupplierChangesToUsageParameters(SystemUsageUpdateParameters parameters)
    {
        var generalHasChange = CheckSupplierChangesToUsageGeneral(parameters.GeneralProperties);
        var gpdrHasChange = CheckSupplierChangesToUsageGdpr(parameters.GDPR);

        return generalHasChange || gpdrHasChange;
    }

    private bool CheckSupplierChangesToUsageGeneral(Maybe<UpdatedSystemUsageGeneralProperties> general)
    {
       return general.HasValue && general.Value.ContainsAITechnology.HasChange;
    }

    private bool CheckSupplierChangesToUsageGdpr(Maybe<UpdatedSystemUsageGDPRProperties> gdpr)
    {
        return gdpr.HasValue && (gdpr.Value.GdprCriticality.HasChange || gdpr.Value.RiskAssessmentResult.HasChange);
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
            SystemUsageUpdateParameters usageParameters => CheckNonSupplierChangesToUsageParameters(usageParameters, entity),
            _ => false
        };
    }

    private bool CheckNonSupplierChangesToUsageParameters(SystemUsageUpdateParameters usageParameters, IEntity entity)
    {
        var generalHasChange = CheckGeneralHasNonSupplierChange(usageParameters.GeneralProperties);
        var organizationUsageHasChange = usageParameters.OrganizationalUsage.HasValue && AnyOptionalValueChangeFieldHasChange(usageParameters.OrganizationalUsage.Value);
        var kleHasChange = usageParameters.KLE.HasValue && AnyOptionalValueChangeFieldHasChange(usageParameters.KLE.Value);
        var externalReferencesHasChange = usageParameters.ExternalReferences.HasValue && ExternalReferencesHasChange(usageParameters.ExternalReferences, entity);
        var rolesHasChange = usageParameters.Roles.HasValue && AnyOptionalValueChangeFieldHasChange(usageParameters.Roles.Value);
        var gdprHasChange = CheckGdprHasNonSupplierChange(usageParameters.GDPR);
        var archivingHasChange = usageParameters.Archiving.HasValue && AnyOptionalValueChangeFieldHasChange(usageParameters.Archiving.Value);
        return generalHasChange || organizationUsageHasChange || kleHasChange || externalReferencesHasChange || rolesHasChange || gdprHasChange || archivingHasChange;
    }

    private bool CheckGdprHasNonSupplierChange(Maybe<UpdatedSystemUsageGDPRProperties> usageParametersGdpr)
    {
        if (usageParametersGdpr.IsNone) return false;
        return AnyOptionalValueChangeFieldHasChange(usageParametersGdpr.Value, _usageGdprSupplierAssociatedProperties);
    }

    private bool CheckGeneralHasNonSupplierChange(Maybe<UpdatedSystemUsageGeneralProperties> usageParametersGeneralProperties)
    {
        if (usageParametersGeneralProperties.IsNone) return false;
        return AnyOptionalValueChangeFieldHasChange(usageParametersGeneralProperties.Value, _usageGeneralSupplierAssociatedProperties);
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
        if (entity is not IHasReferences entityWithReferences) return false;
        
        var existingReferences = entityWithReferences.ExternalReferences;
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
        return AnyOptionalValueChangeFieldHasChange(parameters.Value, _dprOversightSupplierAssociatedProperties);
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

    private bool AnyOptionalValueChangeFieldHasChange(object obj, ISet<string> excludedProperties = null)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            if (excludedProperties != null && excludedProperties.Contains(property.Name)) return false;
            var optionalValueChange = property.GetValue(obj);
            var hasChange = optionalValueChange?.GetType().GetProperty(HasChangePropertyName)?.GetValue(optionalValueChange);
            if (hasChange != null && (bool)hasChange) return true;
        }

        foreach (var field in fields)
        {
            var optionalValueChange = field.GetValue(obj);
            var hasChange = optionalValueChange?.GetType().GetProperty(HasChangePropertyName)?.GetValue(optionalValueChange);
            if (hasChange != null && (bool)hasChange) return true;
        }

        return false;
    }
    private bool IsNullOrEmpty<T>(ICollection<T> collection)
    {
        return collection == null || !collection.Any();
    }
}

