using System;
using System.Threading.Tasks;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.KitosEvents;

namespace Core.ApplicationServices
{
    public class KitosEventPublisherService : IKitosEventPublisherService
    {
        private readonly IKitosHttpClient _httpClient;
        private const string PublishUrl = "api/publish";

        public KitosEventPublisherService(IKitosHttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<Maybe<OperationError>> PublishEvent(KitosEvent eventSomething)
        {
            return await PostEventAsync(eventSomething);
        }

        private async Task<Maybe<OperationError>> PostEventAsync(KitosEvent eventSomething)
        {
            var body = new KitosEventDTO(eventSomething);
            var url = new Uri(PublishUrl);
            await _httpClient.PostAsync(body, url);
            return Maybe<OperationError>.None;
        }
    }
}