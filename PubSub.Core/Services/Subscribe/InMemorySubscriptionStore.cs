using PubSub.Core.Consumers;
using PubSub.Core.Models;
using System.Collections.Concurrent;

namespace PubSub.Core.Services.Subscribe
{
    public class InMemorySubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, IConsumer> _consumersByTopicDictionary = new();

        public void AddCallbackToTopic(string topic, string callback)
        {
            _consumersByTopicDictionary[topic].AddCallbackUrl(callback);
        }

        public void SetConsumerForTopic(string topic, IConsumer consumer)
        {
            _consumersByTopicDictionary[topic] = consumer;
        }

        public IDictionary<string, IConsumer> GetSubscriptions()
        {
            return _consumersByTopicDictionary;
        }
    }
}
