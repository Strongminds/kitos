using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Core.Abstractions.Types;
using Newtonsoft.Json;

namespace Core.ApplicationServices
{
    public class KitosEventPublisherService : IKitosEventPublisherService
    {
        private readonly HttpClient _httpClient;
        private const string PublishUrl = "api/something";

        public KitosEventPublisherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<Maybe<OperationError>> PublishEvent(KitosEvent eventSomething)
        {
            return await PostEventAsync(eventSomething);
        }

        private async Task<Maybe<OperationError>> PostEventAsync(KitosEvent eventSomething)
        {
            var topic = eventSomething.Topic;
            var body = ToPublicationDTO(eventSomething.EventBody);
            var serialized = JsonConvert.SerializeObject(body);
            return await PostRawContents(topic, serialized);
        }

        private async Task<Maybe<OperationError>> PostRawContents(string topic, string message)
        {
            // Create the payload according to the expected DTO structure.
            var payloadObject = new
            {
                Topic = topic,
                Message = message
            };
            var payloadJson = JsonConvert.SerializeObject(payloadObject);

            var response = await PostAsync(payloadJson);
            return Maybe<OperationError>.None;
        }

        private async Task<HttpResponseMessage> PostAsync(string serialized)
        {
            var content = new StringContent(serialized, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(PublishUrl, content);
            return response;
        }

        private object ToPublicationDTO(IEvent eventSomething)
        {
            //TODO: Convert Application level models to DTO models
            return eventSomething;
        }
    }
}