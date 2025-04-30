using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.DomainModel.Repositories;
using PubSub.Core.DomainModel.Serializer;
using PubSub.Core.DomainServices.Consumer;

namespace PubSub.Application.Services.RabbitMQUtils
{
    public interface IRabbitMQConsumerFactory
    {
        IConsumer Create(IRabbitMQConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, IJsonPayloadSerializer payloadSerializer, string topic, ISubscriptionRepositoryProvider subscriptionRepositoryProvider);
    }
}
