using System.Text;

namespace Core.ApplicationServices.Model.KitosEvents;

public class KitosEventDTO
{
    public KitosEventDTO(KitosEvent eventSomething)
    {
        Message = eventSomething.EventBody.ToString();
        Topic = eventSomething.Topic;
    }
    public string Message { get; }
    public string Topic { get; }
}