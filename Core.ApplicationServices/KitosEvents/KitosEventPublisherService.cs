using System;
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
    public void PublishEvent(KitosEvent kitosEvent)
    {
        var dto = new KitosEventDTO(kitosEvent);
        TestMethod(dto);

    }

    private void TestMethod(KitosEventDTO dto)
    {
        var method = Task.Run(() =>
        {
            try
            {
                _httpClient.PostEventAsync(dto);
            }
            catch (Exception ex)
            {
                // ignored
            }
        });
        method.Wait();
    }

}