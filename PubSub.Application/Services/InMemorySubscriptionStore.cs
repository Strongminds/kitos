using System.Collections.Concurrent;
using PubSub.Core.Models;

namespace PubSub.Application.Services
{
    public class InMemorySubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, IConsumer> _consumersByTopicDictionary = new();

        public void AddCallbackToTopic(string topic, Uri callback)
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

        public bool HasTopic(string topic)
        {
            return _consumersByTopicDictionary.ContainsKey(topic);
        }
    }
}
