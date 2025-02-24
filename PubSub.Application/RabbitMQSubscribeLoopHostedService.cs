
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PubSub.Application
{
    public class RabbitMQSubscribeLoopHostedService : BackgroundService, ISubscribeLoopHostedService
    {
        private IEnumerable<Subscription> _subscriptions = [];
        private ISet<string> _topics = new HashSet<string>();
        private readonly IMessageSerializer _messageSerializer;
        private readonly IChannel _channel;
        private readonly ISubscriptionManager _topicManager;

        public RabbitMQSubscribeLoopHostedService(IMessageSerializer messageSerializer, IChannel channel, ISubscriptionManager topicManager)
        {
            _messageSerializer = messageSerializer;
            _channel = channel;
            _topicManager = topicManager;
        }

        public async Task UpdateSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            await _topicManager.Add(subscriptions);
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
            var consumerCallback = GetConsumerCallback(channel);
            foreach (var queue in _topics)
            {
                Console.WriteLine(queue + " is queue");
                await channel.BasicConsumeAsync(queue, autoAck: true, consumer: consumerCallback);
            }
        }

        private IAsyncBasicConsumer GetConsumerCallback(IChannel channel) {
            var callbackUrl = _subscriptions.FirstOrDefault()?.Callback ?? "nourl";

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
