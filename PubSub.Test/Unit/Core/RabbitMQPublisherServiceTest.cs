using Moq;
using PubSub.Core.DomainModel;
using PubSub.Core.ApplicationServices.Serializer;
using PubSub.Test.Base;
using RabbitMQ.Client;
using System.Text.Json;
using PubSub.Application.Services;
using PubSub.Infrastructure.MessageQueue;
namespace PubSub.Test.Unit.Core
{
    public class RabbitMQPublisherServiceTest : TestClassWithIChannelBase
    {
        [Fact]
        public async Task Can_Publish_On_Queue()
        {
            var connectionManager = new Mock<IRabbitMQConnectionManager>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<IChannel>();
            connectionManager.Setup(_ => _.GetConnectionAsync()).ReturnsAsync(connection.Object);
            connection.Setup(_ => _.CreateChannelAsync(null, default)).ReturnsAsync(channel.Object);

            var messageSerializer = new Mock<IJsonPayloadSerializer>();

            var sut = new RabbitMQPublisherService(connectionManager.Object, messageSerializer.Object);

            var jsonString = "\"Test Payload\"";
            var payload = JsonSerializer.Deserialize<JsonElement>(jsonString);
            var publication = new Publication(A<Topic>(), payload);

            await sut.PublishAsync(publication);
            AssertChannelDeclaresQueue(channel);
        }
    }
}
