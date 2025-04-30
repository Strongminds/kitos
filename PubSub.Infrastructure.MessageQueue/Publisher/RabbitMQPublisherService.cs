using PubSub.Core.DomainModel;
using RabbitMQ.Client;
using PubSub.Core.DomainServices.Publisher;
using PubSub.Core.DomainModel.Serializer;
using PubSub.Application.Services.RabbitMQUtils;

namespace PubSub.Infrastructure.MessageQueue.Publisher
{
    public class RabbitMQPublisherService : IPublisherService
    {
        private readonly IRabbitMQConnectionManager _connectionManager;
        private readonly IJsonPayloadSerializer _payloadSerializer;

        public RabbitMQPublisherService(IRabbitMQConnectionManager connectionManager, IJsonPayloadSerializer payloadSerializer)
        {
            _connectionManager = connectionManager;
            _payloadSerializer = payloadSerializer;
        }

        public async Task PublishAsync(Publication publication)
        {
            var topic = publication.Topic;
            var connection = await _connectionManager.GetConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: topic.Name, durable: true, exclusive: false, autoDelete: false);

            var serializedBody = _payloadSerializer.Serialize(publication.Payload);
            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: topic.Name, true, basicProperties: properties, body: serializedBody);
        }
    }
}
