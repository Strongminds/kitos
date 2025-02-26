using PubSub.Core.Consumers;
using System.Collections.Concurrent;

namespace PubSub.Core.Services.Subscribe
{
    public class InMemorySubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, IConsumer> _consumersByTopicDictionary = new();

        public void AddSubscriberToTopic(string topic, string subscriber)
        {
            throw new NotImplementedException();
        }

        public void SetConsumerForTopic(string topic, IConsumer consumer)
        {
            _consumersByTopicDictionary[topic] = consumer;
        }

        public IDictionary<string, IConsumer> GetSubscriptions()
        {
            return (IDictionary<string, IConsumer>) _consumersByTopicDictionary;
        }
    }
}
