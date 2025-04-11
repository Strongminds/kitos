using PubSub.Core.Models;
using PubSub.Core.Services.Notifier;
using PubSub.Core.Services.Serializer;

namespace PubSub.Application.Services
{
    public class RabbitMQSubscriberService : ISubscriberService
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ISubscriberNotifierService _subscriberNotifierService;
        private readonly ISubscriptionStore _subscriptionStore;
        private readonly IRabbitMQConsumerFactory _consumerFactory;
        private readonly IPayloadSerializer _payloadSerializer;
        private readonly IServiceScopeFactory _scopeFactory;

        public RabbitMQSubscriberService(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, ISubscriptionStore subscriptionStore, IRabbitMQConsumerFactory consumerFactory, IPayloadSerializer payloadSerializer, IServiceScopeFactory scopeFactory)
        {
            _connectionManager = connectionManager;
            _subscriberNotifierService = subscriberNotifierService;
            _subscriptionStore = subscriptionStore;
            _consumerFactory = consumerFactory;
            _payloadSerializer = payloadSerializer;
            _scopeFactory = scopeFactory;
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
            var topic = subscription.Topic;
            if (!_subscriptionStore.HasTopic(topic))
            {
                await CreateAndStartNewConsumerAsync(topic);
            }

            _subscriptionStore.AddCallbackToTopic(topic, subscription.Callback);
        }

        private async Task CreateAndStartNewConsumerAsync(string topic)
        {
            var consumer = _consumerFactory.Create(_connectionManager, _subscriberNotifierService, _payloadSerializer, topic, _scopeFactory);
            _subscriptionStore.SetConsumerForTopic(topic, consumer);
            await consumer.StartListeningAsync();
        }
    }
}
