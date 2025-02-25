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

        public RabbitMQSubscribeLoopHostedService(IMessageSerializer messageSerializer, IChannel channel, ISubscriptionManager topicManager)
        {
            _messageSerializer = messageSerializer;
            _channel = channel;
            _subscriptionManager = topicManager;
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
                var callbackUrls = subscriptions.GetValueOrDefault(topic);
                foreach (var url in callbackUrls!)
                {
                    var consumerCallback = GetConsumerCallback(channel, url);
                    await channel.BasicConsumeAsync(topic.Name, autoAck: true, consumer: consumerCallback);
                }
            }
        }

        private IAsyncBasicConsumer GetConsumerCallback(IChannel channel, string callbackUrl) {
            var consumerCallback = new AsyncEventingBasicConsumer(channel);
            consumerCallback.ReceivedAsync += async (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = _messageSerializer.Deserialize(body);
                var json = JsonContent.Create(message);
                var content = new StringContent($"\"{message}\"", Encoding.UTF8, "application/json");
                var http = new HttpClient();
                var res = await http.PostAsync(callbackUrl, content);
                Console.WriteLine($"Received {message}");
            };
            return consumerCallback;
        }
    }
}
