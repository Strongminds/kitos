using System;
namespace Core.ApplicationServices.Model.KitosEvents;

public class SystemDataProcessorChangeEventBodyModel : IEventBody
{
    public Guid SystemUuid { get; set; }
    public Guid? DataProcessorUuid { get; set; }
    public string DataProcessorName { get; set; }

    public string Type()
    {
        return "DataProcessorChange";
    }
}