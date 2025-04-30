using PubSub.Application.Services.RabbitMQUtils;
using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.DomainModel.Repositories;
using PubSub.Core.DomainModel.Serializer;
using PubSub.Core.DomainServices.Consumer;

namespace PubSub.Infrastructure.MessageQueue.Consumer
{
    public class RabbitMQConsumerFactory : IRabbitMQConsumerFactory
    {
        public IConsumer Create(IRabbitMQConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, IJsonPayloadSerializer payloadSerializer, string topic, ISubscriptionRepositoryProvider subscriptionRepositoryProvider)
        {
            return new RabbitMQConsumer(connectionManager, subscriberNotifierService, payloadSerializer, topic, subscriptionRepositoryProvider);
        }
    }
}
