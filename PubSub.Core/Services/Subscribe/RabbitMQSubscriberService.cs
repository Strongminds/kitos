using System.Collections.Concurrent;
using PubSub.Core.Consumers;
using PubSub.Core.Managers;
using PubSub.Core.Models;
using PubSub.Core.Services.Notifier;

namespace PubSub.Core.Services.Subscribe
{
    public class RabbitMQSubscriberService : ISubscriberService
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ISubscriberNotifierService _subscriberNotifierService;
        private readonly ISubscriptionStore _subscriptionStore;
        private readonly IRabbitMQConsumerFactory _consumerFactory;

        public RabbitMQSubscriberService(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, ISubscriptionStore subscriptionStore, IRabbitMQConsumerFactory consumerFactory)
        {
            _connectionManager = connectionManager;
            _subscriberNotifierService = subscriberNotifierService;
            _subscriptionStore = subscriptionStore;
            _consumerFactory = consumerFactory;
        }

        public async Task AddSubscriptionsAsync(IEnumerable<Subscription> subscriptions)
        {
            foreach (var subscription in subscriptions)
            {
                await UpdateConsumersAsync(subscription);
            }
        }

        private async Task UpdateConsumersAsync(Subscription subscription)
        {
            foreach (var topic in subscription.Topics)
            {
                if (!_subscriptionStore.GetSubscriptions().ContainsKey(topic))
                    {
                        await CreateAndStartNewConsumerAsync(topic);
                    }

                _subscriptionStore.AddCallbackToTopic(topic, subscription.Callback);
            }
        }

        private async Task CreateAndStartNewConsumerAsync(Topic topic)
        {
            var consumer = _consumerFactory.Create(_connectionManager, _subscriberNotifierService, topic);
            _subscriptionStore.SetConsumerForTopic(topic, consumer);
            await consumer.StartListeningAsync();
        }
    }
}
