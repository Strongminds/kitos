using System;

namespace Core.ApplicationServices.Model.KitosEvents;

public class SystemChangeEventModel : IEvent
{
    public Guid SystemUuid { get; set; }
    public string SystemName { get; set; }
    public Guid? RightsHolderUuid { get; set; }
    public string RightsHolderName { get; set; }
}
