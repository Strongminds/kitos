using PubSub.Core.Models;
using PubSub.Core.Services.Notifier;
using PubSub.Core.Services.Serializer;
using PubSub.DataAccess;

namespace PubSub.Application.Services
{
    public interface IRabbitMQConsumerFactory
    {
        IConsumer Create(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, IPayloadSerializer payloadSerializer, string topic, IServiceScopeFactory scopeFactory);
    }
}
