
using RabbitMQ.Client;

namespace PubSub.Application
{
    public class RabbitMQPublisher : IPublisher
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly IChannel _channel;

        public RabbitMQPublisher(IMessageSerializer messageSerializer, IChannel channel)
        {
            _messageSerializer = messageSerializer;
            _channel = channel;
        }

        public async Task Publish(Publication publication)
        {
            var queue = publication.Queue;
            var message = publication.Message;

            await _channel.QueueDeclareAsync(queue); //not needed?
            var serializedBody = _messageSerializer.Serialize(message);

            await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: queue, body: serializedBody);
            Console.WriteLine($"Published {message}");
        }
    }
}
