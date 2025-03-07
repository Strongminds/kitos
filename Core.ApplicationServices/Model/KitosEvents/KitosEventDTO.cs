using Newtonsoft.Json;

namespace Core.ApplicationServices.Model.KitosEvents;

public class KitosEventDTO
{
    public KitosEventDTO(KitosEvent kitosEvent)
    {

        Message = JsonConvert.SerializeObject(kitosEvent.EventBody);
        Topic = kitosEvent.Topic;
    }
    public string Message { get; }
    public string Topic { get; }
}