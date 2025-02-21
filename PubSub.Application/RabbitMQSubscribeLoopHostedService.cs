
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PubSub.Application
{
    public class RabbitMQSubscribeLoopHostedService : BackgroundService, ISubscribeLoopHostedService
    {
        private readonly IConnectionFactory _connectionFactory;
        private IEnumerable<Subscription> _subscriptions = [];
        private ISet<string> _queues = new HashSet<string>();
        private readonly IMessageSerializer _messageSerializer;

        public RabbitMQSubscribeLoopHostedService(IConnectionFactory connectionFactory, IMessageSerializer messageSerializer)
        {
            _connectionFactory = connectionFactory;
            _messageSerializer = messageSerializer;
        }

        public async Task UpdateSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            _subscriptions = subscriptions;
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            var queuesSet = new HashSet<string>();
            foreach (var subscription in subscriptions)
            {
                foreach (var queue in subscription.Queues)
                {
                    queuesSet.Add(queue);
                }
            }
            _queues = queuesSet;

            foreach (var queue in queuesSet)
            {
                await channel.QueueDeclareAsync(queue);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => StartSubscribeLoop(stoppingToken), stoppingToken);
        }

        private async Task StartSubscribeLoop(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ConsumeNextMessage();
            }
        }

        private async Task ConsumeNextMessage()
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            var consumerCallback = GetConsumerCallback(channel);

            foreach (var queue in _queues)
            {
                await channel.BasicConsumeAsync(queue, autoAck: true, consumer: consumerCallback);
            }
        }

        private IAsyncBasicConsumer GetConsumerCallback(IChannel channel) {
            var consumerCallback = new AsyncEventingBasicConsumer(channel);
            consumerCallback.ReceivedAsync += (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = _messageSerializer.Deserialize(body);
                Console.WriteLine($"Received {message}");
                return Task.CompletedTask;
            };
            return consumerCallback;
        }
    }
}
