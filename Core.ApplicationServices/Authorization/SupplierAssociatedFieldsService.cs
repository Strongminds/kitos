using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Abstractions.Types;
using Core.ApplicationServices.GDPR;
using Core.ApplicationServices.Model.GDPR.Write;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    private readonly IDataProcessingRegistrationApplicationService _dataProcessingRegistrationApplicationService;
    private readonly string _hasChangePropertyName = "HasChange";

    public SupplierAssociatedFieldsService(IDataProcessingRegistrationApplicationService dataProcessingRegistrationApplicationService)
    {
        _dataProcessingRegistrationApplicationService = dataProcessingRegistrationApplicationService;
    }
    public bool RequestsChangesToSupplierAssociatedFields(DataProcessingRegistrationModificationParameters parameters)
    {
        //todo add remaining target fields when miol responds
        var oversight = parameters.Oversight;
        return oversight.HasValue && oversight.Value.IsOversightCompleted.HasChange;
    }

    public bool RequestsChangesToNonSupplierAssociatedFields(DataProcessingRegistrationModificationParameters parameters, Guid dataProcessingUuid)
    {
        var nameHasChange = parameters.Name.HasChange;
        var generalHasChange = parameters.General.HasValue && AnyOptionalValueChangeFieldHasChange(parameters.General.Value);
        var rolesHasChange = parameters.Roles.HasValue && AnyOptionalValueChangeFieldHasChange(parameters.Roles.Value);
        var systemUsageUuidsHasChange = SystemUsageUuidsHasChange(parameters.SystemUsageUuids, dataProcessingUuid);
        return nameHasChange || generalHasChange || rolesHasChange || systemUsageUuidsHasChange;
    }

    private bool SystemUsageUuidsHasChange(Maybe<IEnumerable<Guid>> updatedSystemUsageUuids, Guid dataProcessingUuid)
    {
        var dataProcessingRegistrationResult = _dataProcessingRegistrationApplicationService.GetByUuid(dataProcessingUuid);
        return dataProcessingRegistrationResult.Match(dataProcessingRegistration =>
        {
            if (updatedSystemUsageUuids.IsNone && dataProcessingRegistration.SystemUsages == null) return false;
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
}

