using PubSub.Application.Common;
using RabbitMQ.Client;

namespace PubSub.Application.Publish
{
    public class RabbitMQPublisher : IPublisher
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly IChannel _channel;
        private readonly IMessageBusTopicManager _messageBusTopicManager;

        public RabbitMQPublisher(IMessageSerializer messageSerializer, IChannel channel, IMessageBusTopicManager messageBusTopicManager)
        {
            _messageSerializer = messageSerializer;
            _channel = channel;
            _messageBusTopicManager = messageBusTopicManager;
        }

        public async Task Publish(Publication publication)
        {
            var topic = publication.Queue;
            var message = publication.Message;

            await _messageBusTopicManager.Add(new Topic { Name = topic });
            var serializedBody = _messageSerializer.Serialize(message);

            await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: topic, body: serializedBody);
            Console.WriteLine($"Published {message}");
        }
    }
}
