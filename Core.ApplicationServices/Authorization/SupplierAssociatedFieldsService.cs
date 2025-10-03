using System;
using System.Reflection;
using Core.ApplicationServices.GDPR;
using Core.ApplicationServices.Model.GDPR.Write;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    private readonly IDataProcessingRegistrationApplicationService _dataProcessingRegistrationApplicationService;

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
        return nameHasChange || generalHasChange || rolesHasChange;
    }

    private bool AnyOptionalValueChangeFieldHasChange(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj)); 

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var optionalValueChange = property.GetValue(obj);
            var hasChange = optionalValueChange?.GetType().GetProperty("HasChange")?.GetValue(optionalValueChange);
            if (hasChange != null && (bool)hasChange) return true;
        }

        foreach (var field in fields)
        {
            var optionalValueChange = field.GetValue(obj);
            var hasChange = optionalValueChange?.GetType().GetProperty("HasChange")?.GetValue(optionalValueChange);
            if (hasChange != null && (bool)hasChange) return true;
        }

        return false;
    }
}

