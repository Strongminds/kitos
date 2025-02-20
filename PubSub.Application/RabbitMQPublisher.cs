
using RabbitMQ.Client;
using System.Text;

namespace PubSub.Application
{
    public class RabbitMQPublisher : IPublisher
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IMessageSerializer _messageSerializer;

        public RabbitMQPublisher(IConnectionFactory connectionFactory, IMessageSerializer messageSerializer)
        {
            _connectionFactory = connectionFactory;
            _messageSerializer = messageSerializer;
        }

        public async Task Publish(string queue, string message)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue);
            var serializedBody = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queue, body: serializedBody);
            Console.WriteLine($"Published {message}");
        }
    }
}
