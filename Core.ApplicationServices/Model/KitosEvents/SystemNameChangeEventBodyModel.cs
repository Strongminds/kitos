using System;

namespace Core.ApplicationServices.Model.KitosEvents;

public class SystemNameChangeEventBodyModel : IEventBody
{
    public Guid SystemUuid { get; set; }
    public string SystemName { get; set; }

    public string Type()
    {
        return "NameChange";
    }
}
