using System.Collections.Concurrent;
using PubSub.Core.Consumers;
using PubSub.Core.Managers;
using PubSub.Core.Models;

namespace PubSub.Core.Services
{
    public class RabbitMQSubscriberService : ISubscriberService
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ISubscriberNotifierService _subscriberNotifierService;
        private readonly ConcurrentDictionary<string, RabbitMQConsumer> _consumers = new();

        public RabbitMQSubscriberService( IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService)
        {
            _connectionManager = connectionManager;
            _subscriberNotifierService = subscriberNotifierService;
        }

        public async Task SubscribeToQueuesAsync(IEnumerable<Subscription> subscriptions)
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
                if (!_consumers.ContainsKey(topic))
                {
                    await CreateNewConsumerAsync(topic);
                }

                _consumers[topic].AddCallbackUrl(subscription.Callback);
            }
        }

        private async Task CreateNewConsumerAsync(string topic)
        {
            var consumer = new RabbitMQConsumer(_connectionManager, _subscriberNotifierService, topic);
            _consumers[topic] = consumer;
            await consumer.StartListeningAsync();
        }
    }
}
