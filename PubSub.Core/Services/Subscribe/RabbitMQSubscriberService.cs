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
        private readonly ConcurrentDictionary<string, RabbitMQConsumer> _consumersByTopicDictionary = new();

        public RabbitMQSubscriberService(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService)
        {
            _connectionManager = connectionManager;
            _subscriberNotifierService = subscriberNotifierService;
        }

        public async Task SubscribeToTopicsAsync(IEnumerable<Subscription> subscriptions)
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
                if (!_consumersByTopicDictionary.ContainsKey(topic))
                {
                    await CreateAndStartNewConsumerAsync(topic);
                }

                _consumersByTopicDictionary[topic].AddCallbackUrl(subscription.Callback);
            }
        }

        private async Task CreateAndStartNewConsumerAsync(string topic)
        {
            var consumer = new RabbitMQConsumer(_connectionManager, _subscriberNotifierService, topic);
            _consumersByTopicDictionary[topic] = consumer;
            await consumer.StartListeningAsync();
        }
    }
}
