﻿using PubSub.Core.Consumers;
using PubSub.Core.Models;
using System.Collections.Concurrent;

namespace PubSub.Core.Services.Subscribe
{
    public class InMemorySubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<Topic, IConsumer> _consumersByTopicDictionary = new();

        public void AddCallbackToTopic(Topic topic, string callback)
        {
            _consumersByTopicDictionary[topic].AddCallbackUrl(callback);
        }

        public void SetConsumerForTopic(Topic topic, IConsumer consumer)
        {
            _consumersByTopicDictionary[topic] = consumer;
        }

        public IDictionary<Topic, IConsumer> GetSubscriptions()
        {
            return _consumersByTopicDictionary;
        }
    }
}
