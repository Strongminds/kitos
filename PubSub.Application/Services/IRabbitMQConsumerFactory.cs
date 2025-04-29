using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Serializer;

namespace PubSub.Application.Services
{
    public interface IRabbitMQConsumerFactory
    {
        IConsumer Create(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, IPayloadSerializer payloadSerializer, string topic, IServiceScopeFactory scopeFactory);
    }
}
