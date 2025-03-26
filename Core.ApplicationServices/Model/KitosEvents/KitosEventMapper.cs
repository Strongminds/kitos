using Newtonsoft.Json;

namespace Core.ApplicationServices.Model.KitosEvents;

public class KitosEventMapper : IKitosEventMapper
{
    public KitosEventDTO MapKitosEventToDTO(KitosEvent kitosEvent)
    {
        var message = kitosEvent.EventBody.ToKeyValuePairs();
        var topic = kitosEvent.Topic + kitosEvent.EventBody.Type();
        return new KitosEventDTO(message, topic);
    }
}