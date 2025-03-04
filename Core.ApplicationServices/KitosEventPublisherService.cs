using System.Threading.Tasks;
using Core.Abstractions.Types;

namespace Core.ApplicationServices
{
    public class KitosEventPublisherService : IKitosEventPublisherService
    {
        public async Task<Maybe<OperationError>> PublishEvent(KitosEvent eventSomething)
        {
            return await PostEventAsync(eventSomething);
        }

        private async Task<Maybe<OperationError>> PostEventAsync(KitosEvent eventSomething)
        {
            var topic = eventSomething.Topic;
            var body = ToPublicationDTO(eventSomething.EventBody);
            return await PostRawContents(topic, body);
        }

        private async Task<Maybe<OperationError>> PostRawContents(string topic, object body)
        {
            return Maybe<OperationError>.None;
        }

        private object ToPublicationDTO(IEvent eventSomething)
        {
            return eventSomething;
        }
    }
}