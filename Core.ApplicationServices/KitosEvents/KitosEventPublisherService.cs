using Core.ApplicationServices.Model.KitosEvents;
using System.Threading.Tasks;

namespace Core.ApplicationServices.KitosEvents;

public class KitosEventPublisherService : IKitosEventPublisherService
{
    private readonly IHttpEventPublisher _httpClient;

    public KitosEventPublisherService(IHttpEventPublisher httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task PublishEvent(KitosEvent kitosEvent)
    {
        var dto = new KitosEventDTO(kitosEvent);
        await _httpClient.PostEventAsync(dto);
    }
}