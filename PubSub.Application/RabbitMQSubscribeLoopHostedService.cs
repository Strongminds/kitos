
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PubSub.Application
{
    public class RabbitMQSubscribeLoopHostedService : BackgroundService, ISubscribeLoopHostedService
    {
        private IEnumerable<Subscription> _subscriptions = [];
        private ISet<string> _queues = new HashSet<string>();
        private readonly IMessageSerializer _messageSerializer;
        private readonly IConnection _connection;
        private IChannel? _channel;

        public RabbitMQSubscribeLoopHostedService(IConnection connection, IMessageSerializer messageSerializer)
        {
            _connection = connection;
    
            _messageSerializer = messageSerializer;
        }

        public async Task UpdateSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            if (_channel == null) _channel = await _connection.CreateChannelAsync();
            _subscriptions = subscriptions;

            var queuesSet = new HashSet<string>();
            foreach (var subscription in subscriptions)
            {
                foreach (var queue in subscription.Queues)
                {
                    queuesSet.Add(queue);
                }
            }

            foreach (var queue in queuesSet)
            {
                await _channel.QueueDeclareAsync(queue);
            }
            _queues = queuesSet;
            
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => RunSubscribeLoop(stoppingToken), stoppingToken);
        }

        private async Task RunSubscribeLoop(CancellationToken stoppingToken) {
            if (_channel == null) _channel = await _connection.CreateChannelAsync();
            while (!stoppingToken.IsCancellationRequested)
            {
                await ConsumeNextMessage(_channel);
            }
        }

        private async Task ConsumeNextMessage(IChannel channel)
        {
            var consumerCallback = GetConsumerCallback(channel);
            foreach (var queue in _queues)
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
