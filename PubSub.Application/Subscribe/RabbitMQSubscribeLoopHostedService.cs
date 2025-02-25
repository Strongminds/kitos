using PubSub.Application.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PubSub.Application.Subscribe
{
    public class RabbitMQSubscribeLoopHostedService : BackgroundService, ISubscribeLoopHostedService
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly IChannel _channel;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ISubscriberNotifier _subscriberNotifier;

        public RabbitMQSubscribeLoopHostedService(IMessageSerializer messageSerializer, IChannel channel, ISubscriptionManager topicManager, ISubscriberNotifier subscriberNotifier)
        {
            _messageSerializer = messageSerializer;
            _channel = channel;
            _subscriptionManager = topicManager;
            _subscriberNotifier = subscriberNotifier;
        }

        public async Task UpdateSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            await _subscriptionManager.Add(subscriptions);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => RunSubscribeLoop(stoppingToken), stoppingToken);
        }

        private async Task RunSubscribeLoop(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ConsumeNextMessage(_channel);
            }
        }

        private async Task ConsumeNextMessage(IChannel channel)
        {
            var subscriptions = _subscriptionManager.Get();
            foreach (var topic in subscriptions.Keys)
            {
                if (subscriptions.TryGetValue(topic, out var callbackUrls))
                {
                    var consumerCallback = GetConsumerCallback(channel, callbackUrls ?? new HashSet<string>());
                    await channel.BasicConsumeAsync(topic, autoAck: true, consumer: consumerCallback);
                }
            }
        }

        private IAsyncBasicConsumer GetConsumerCallback(IChannel channel, HashSet<string> callbackUrls) {
            var consumerCallback = new AsyncEventingBasicConsumer(channel);
            consumerCallback.ReceivedAsync += async (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = _messageSerializer.Deserialize(body);
                var content = new StringContent($"\"{message}\"", Encoding.UTF8, "application/json");
                var http = new HttpClient();
                foreach (var callbackUrl in callbackUrls)
                {
                    await _subscriberNotifier.Notify(message, callbackUrl);
                }
                Console.WriteLine($"Received {message}");
            };
            return consumerCallback;
        }
    }
}
