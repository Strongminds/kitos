
using PubSub.Application.Common;
using RabbitMQ.Client;

namespace PubSub.Application
{
    public class RabbitMQMessageBusTopicManager : IMessageBusTopicManager
    {
        private readonly IChannel _channel;

        public RabbitMQMessageBusTopicManager(IChannel channel)
        {
            _channel = channel;
        }

        public async Task Add(Topic topic)
        {
            await _channel.QueueDeclareAsync(topic.Name);
        }
    }
}
