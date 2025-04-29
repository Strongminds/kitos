using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Repositories;
using PubSub.Core.ApplicationServices.Serializer;
using PubSub.Core.DomainServices.Subscriber;

namespace PubSub.Application.Services
{
    public interface IRabbitMQConsumerFactory
    {
        IConsumer Create(IRabbitMQConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, IJsonPayloadSerializer payloadSerializer, string topic, ISubscriptionRepositoryProvider subscriptionRepositoryProvider);
    }
}
