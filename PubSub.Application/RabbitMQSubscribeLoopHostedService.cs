
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PubSub.Application
{
    public class RabbitMQSubscribeLoopHostedService : BackgroundService, ISubscribeLoopHostedService
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly string _queue;
        private readonly IMessageSerializer _messageSerializer;

        public RabbitMQSubscribeLoopHostedService(IConnectionFactory connectionFactory, string queue, IMessageSerializer messageSerializer)
        {
            _connectionFactory = connectionFactory;
            _queue = queue;
            _messageSerializer = messageSerializer;
        }

        public void UpdateSubscriptions(IList<Subscription> subscriptions)
        {
            throw new NotImplementedException();
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
            await channel.QueueDeclareAsync(_queue);

            var consumerCallback = new AsyncEventingBasicConsumer(channel);
            consumerCallback.ReceivedAsync += (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = _messageSerializer.Deserialize(body);
                Console.WriteLine($"Received {message}");
                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(_queue, autoAck: true, consumer: consumerCallback);

        }
    }
}
