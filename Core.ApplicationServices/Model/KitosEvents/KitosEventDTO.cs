using Newtonsoft.Json;

namespace Core.ApplicationServices.Model.KitosEvents;

public class KitosEventDTO
{
    public KitosEventDTO(KitosEvent eventSomething)
    {

        Message = JsonConvert.SerializeObject(eventSomething.EventBody);
        Topic = eventSomething.Topic;
    }
    public string Message { get; }
    public string Topic { get; }
}