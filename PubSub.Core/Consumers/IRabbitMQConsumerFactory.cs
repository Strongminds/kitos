using PubSub.Core.Managers;
using PubSub.Core.Services.Notifier;
using PubSub.Core.Models;

namespace PubSub.Core.Consumers
{
    public interface IRabbitMQConsumerFactory
    {
        RabbitMQConsumer Create(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, Topic topic);
    }
}
