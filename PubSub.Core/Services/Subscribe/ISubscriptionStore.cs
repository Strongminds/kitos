﻿using PubSub.Core.Consumers;

namespace PubSub.Core.Services.Subscribe
{
    public interface ISubscriptionStore
    {
        void AddSubscriberToTopic(string topic, string subscriber);

        void AddTopicWithConsumer(string topic, IConsumer consumer);

        IDictionary<string, IConsumer> GetSubscriptions();
    }
}
