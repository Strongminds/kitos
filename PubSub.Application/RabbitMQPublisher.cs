
using RabbitMQ.Client;

namespace PubSub.Application
{
    public class RabbitMQPublisher : IPublisher
    {
        private readonly IConnection _connection;
        private readonly IMessageSerializer _messageSerializer;

        public RabbitMQPublisher(IConnection connection, IMessageSerializer messageSerializer)
        {
            _connection = connection;
            _messageSerializer = messageSerializer;
        }

        public async Task Publish(Publication publication)
        {
            var queue = publication.Queue;
            var message = publication.Message;
            using var channel = await _connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue); //not needed?
            var serializedBody = _messageSerializer.Serialize(message);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queue, body: serializedBody);
            Console.WriteLine($"Published {message}");
        }
    }
}
