using System;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Shared;

namespace Core.ApplicationServices.Model.KitosEvents;

public class SystemNameChangeEventBodyModel : IEventBody
{
    public Guid SystemUuid { get; set; }
    public OptionalValueChange<string> SystemName { get; set; } = OptionalValueChange<string>.None;
    public Dictionary<string, object> ToKeyValuePairs()
    {
        var keyValuePairs = new Dictionary<string, object> { { nameof(SystemUuid), SystemUuid } };

        if (SystemName.HasChange) keyValuePairs.Add(nameof(SystemName), SystemName.NewValue);

        return keyValuePairs;
    }

    public string Type()
    {
        return "NameChange";
    }
}
