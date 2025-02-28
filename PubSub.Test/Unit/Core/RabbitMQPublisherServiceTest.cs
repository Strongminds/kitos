﻿using Moq;
using PubSub.Core.Managers;
using PubSub.Core.Models;
using PubSub.Core.Services.Publisher;
using PubSub.Core.Services.Serializer;
using PubSub.Test.Base.Tests.Toolkit.Patterns;
using RabbitMQ.Client;

namespace PubSub.Test.Unit.Core
{
    public class RabbitMQPublisherServiceTest: WithAutoFixture
    {
        [Fact]
        public async Task Can_Publish_On_Queue()
        {
            var connectionManager = new Mock<IConnectionManager>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<IChannel>();
            connectionManager.Setup(_ => _.GetConnectionAsync()).ReturnsAsync(connection.Object);
            connection.Setup(_ => _.CreateChannelAsync(null, default)).ReturnsAsync(channel.Object);

            var messageSerializer = new Mock<IMessageSerializer>();

            var sut = new RabbitMQPublisherService(connectionManager.Object, messageSerializer.Object);
            var publication = A<Publication>();

            await sut.Publish(publication);
            channel.Verify(ch => ch.QueueDeclareAsync(
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
            ), Times.Once);
        }
    }
}
