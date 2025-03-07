using System;
using System.Collections.Generic;
using Core.ApplicationServices.Model.Shared;

namespace Core.ApplicationServices.Model.KitosEvents;

public class SystemChangeEventModel : IEvent
{
    public Guid SystemUuid { get; set; }
    public OptionalValueChange<string> SystemName { get; set; } = OptionalValueChange<string>.None;
    public OptionalValueChange<Guid?> RightsHolderUuid { get; set; } = OptionalValueChange<Guid?>.None;
    public OptionalValueChange<string> RightsHolderName { get; set; } = OptionalValueChange<string>.None;
    public Dictionary<string, object> ToKeyValuePairs()
    {
        var keyValuePairs = new Dictionary<string, object> { { nameof(SystemUuid), SystemUuid } };

        if (SystemName.HasChange) keyValuePairs.Add(nameof(SystemName), SystemName.NewValue);
        if (RightsHolderUuid.HasChange) keyValuePairs.Add(nameof(RightsHolderUuid), RightsHolderUuid.NewValue);
        if (RightsHolderName.HasChange) keyValuePairs.Add(nameof(RightsHolderName), RightsHolderName.NewValue);
        
        return keyValuePairs;
    }

}
