
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PubSub.Application
{
    public class RabbitMQSubscribeLoopHostedService : BackgroundService, ISubscribeLoopHostedService
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IEnumerable<Subscription> _subscriptions = new List<Subscription>();
        private readonly IMessageSerializer _messageSerializer;

        public RabbitMQSubscribeLoopHostedService(IConnectionFactory connectionFactory, IMessageSerializer messageSerializer)
        {
            _connectionFactory = connectionFactory;
            _messageSerializer = messageSerializer;
        }

        public async Task UpdateSubscriptions(IList<Subscription> subscriptions)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            var allQueues = new HashSet<string>();
            foreach (var subscription in subscriptions)
            {
                foreach (var queue in subscription.Queues)
                {
                    allQueues.Add(queue);
                }
            }

            foreach (var queue in allQueues)
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

            await channel.BasicConsumeAsync(_queue, autoAck: true, consumer: consumerCallback);
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
