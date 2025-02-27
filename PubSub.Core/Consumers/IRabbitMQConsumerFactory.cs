﻿using PubSub.Core.Managers;
using PubSub.Core.Services.Notifier;
using PubSub.Core.Models;
using PubSub.Core.Services.Serializer;

namespace PubSub.Core.Consumers
{
    public interface IRabbitMQConsumerFactory
    {
        RabbitMQConsumer Create(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, IMessageSerializer messageSerializer, Topic topic);
    }
}
