using PubSub.Core.Managers;
using PubSub.Core.Services.Notifier;

namespace PubSub.Core.Consumers
{
    public class RabbitMQConsumerFactory : IRabbitMQConsumerFactory
    {
        public RabbitMQConsumer Create(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, string queueName)
        {
            return new RabbitMQConsumer(connectionManager, subscriberNotifierService, queueName);
        }
    }
}
