using PubSub.Core.Managers;
using PubSub.Core.Models;
using PubSub.Core.Services.Notifier;

namespace PubSub.Core.Consumers
{
    public class RabbitMQConsumerFactory : IRabbitMQConsumerFactory
    {
        public RabbitMQConsumer Create(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, Topic topic)
        {
            return new RabbitMQConsumer(connectionManager, subscriberNotifierService, topic.Name);
        }
    }
}
