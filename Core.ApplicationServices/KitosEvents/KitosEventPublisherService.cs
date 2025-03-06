using Core.Abstractions.Types;
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
    public async Task<Maybe<OperationError>> PublishEvent(KitosEvent eventSomething)
    {
        var dto = new KitosEventDTO(eventSomething);
        await _httpClient.PostEventAsync(dto);
        return Maybe<OperationError>.None;
    }
}