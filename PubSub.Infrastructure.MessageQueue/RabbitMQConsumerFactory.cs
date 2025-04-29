using PubSub.Application.Services;
using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Serializer;
using PubSub.Core.DomainModel.Repositories;
using PubSub.Core.DomainServices.Subscriber;

namespace PubSub.Infrastructure.MessageQueue
{
    public class RabbitMQConsumerFactory : IRabbitMQConsumerFactory
    {
        public IConsumer Create(IRabbitMQConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, IJsonPayloadSerializer payloadSerializer, string topic, ISubscriptionRepositoryProvider subscriptionRepositoryProvider)
        {
            return new RabbitMQConsumer(connectionManager, subscriberNotifierService, payloadSerializer, topic, subscriptionRepositoryProvider);
        }
    }
}
