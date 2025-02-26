﻿using PubSub.Core.Managers;
using PubSub.Core.Services.Serializer;
using RabbitMQ.Client;

namespace PubSub.Core.Services.Publish
{
    public class RabbitMQPublisherService : IPublisherService
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IMessageSerializer _messageSerializer;

        public RabbitMQPublisherService(IConnectionManager connectionManager, IMessageSerializer messageSerializer)
        {
            _connectionManager = connectionManager;
            _messageSerializer = messageSerializer;
        }

        public async Task Publish(string topic, string message)
        {
            var connection = await _connectionManager.GetConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: topic, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var serializedBody = _messageSerializer.Serialize(message);
            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: topic, body: serializedBody);
        }
    }
}
