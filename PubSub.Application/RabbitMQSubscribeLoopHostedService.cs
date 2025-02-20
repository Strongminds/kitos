
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PubSub.Application
{
    public class RabbitMQSubscribeLoopHostedService : BackgroundService, ISubscribeLoopHostedService
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly string _queue;

        public RabbitMQSubscribeLoopHostedService(IConnectionFactory connectionFactory, string queue)
        {
            _connectionFactory = connectionFactory;
            _queue = queue;
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
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received {message}");
                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(_queue, autoAck: true, consumer: consumerCallback);

        }
    }
}
