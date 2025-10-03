using System;
using System.Linq;
using System.Reflection;
using Core.ApplicationServices.Model.GDPR.Write;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    public bool RequestsChangesToSupplierAssociatedFields(DataProcessingRegistrationModificationParameters parameters)
    {
        //todo add remaining target fields when miol responds
        var oversight = parameters.Oversight;
        return oversight.HasValue && oversight.Value.IsOversightCompleted.HasChange;
    }

    public bool RequestsChangesToNonSupplierAssociatedFields(DataProcessingRegistrationModificationParameters parameters)
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

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            if (value == null) continue;
            var hasChangeMethod = value.GetType().GetMethod("HasChange");
            if (hasChangeMethod == null) continue;
            var hasChange = (bool)hasChangeMethod.Invoke(value, null);
            if (hasChange) return true;
        }
        return false;
    }
}

