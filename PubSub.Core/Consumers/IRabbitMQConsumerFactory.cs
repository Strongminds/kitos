using PubSub.Core.Managers;
using PubSub.Core.Services.Notifier;

namespace PubSub.Core.Consumers
{
    public interface IRabbitMQConsumerFactory
    {
        RabbitMQConsumer Create(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, string queueName);
    }
}
