﻿using PubSub.Core.Managers;
using PubSub.Core.Services.Serializer;
using PubSub.Core.Models;
using RabbitMQ.Client;

namespace PubSub.Core.Services.Publisher
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

        public async Task PublishAsync(Publication publication)
        {
            var topic = publication.Topic;
            var connection = await _connectionManager.GetConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: topic.Name, durable: true, exclusive: false, autoDelete: false);

            var serializedBody = _messageSerializer.Serialize(publication.Message);
            Console.WriteLine($"ready to publish {publication.Message}");
            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: topic.Name, body: serializedBody); //kommer fra  ny IPublsihser med 2 impl: rigtig kalder dette, stub gør intet men registrerer i state at den blev kaldt
        }
    }
}
