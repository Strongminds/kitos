using Moq;
using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Serializer;
using RabbitMQ.Client;
using PubSub.Test.Base;
using Microsoft.Extensions.DependencyInjection;
using PubSub.Core.DomainServices.RabbitMQConnection;
using PubSub.Application.Services;

namespace PubSub.Test.Unit.Core
{
    public class RabbitMQConsumerTest : TestClassWithIChannelBase
    {
        [Fact]
        public async Task Can_Consume_On_Channel()
        {
            var mockConnectionManager = new Mock<IRabbitMQConnectionManager>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<IChannel>();
            var repo = new Mock<IServiceScopeFactory>();
            mockConnectionManager.Setup(_ => _.GetConnectionAsync()).ReturnsAsync(connection.Object);
            connection.Setup(_ => _.CreateChannelAsync(null, default)).ReturnsAsync(channel.Object);
            var messageSerializer = new Mock<IPayloadSerializer>();

            var mockSubscriberNotifierService = new Mock<ISubscriberNotifierService>();
            var topic = A<string>();

            var sut = new RabbitMQConsumer(
                mockConnectionManager.Object,
                mockSubscriberNotifierService.Object,
                messageSerializer.Object,
                topic,
                repo.Object
            );

            await sut.StartListeningAsync();
            AssertChannelDeclaresQueue(channel);
            AssertChannelConsumes(channel);
        }
    }
}
