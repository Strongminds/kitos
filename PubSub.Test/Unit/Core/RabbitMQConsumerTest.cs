using Moq;
using PubSub.Core.Consumers;
using PubSub.Core.Managers;
using PubSub.Core.Services.Notifier;
using PubSub.Core.Services.Serializer;
using RabbitMQ.Client;
using PubSub.Core.Models;
using PubSub.Test.Base.Tests.Toolkit.Patterns;

namespace PubSub.Test.Unit.Core
{
    public class RabbitMQConsumerTest: WithAutoFixture
    {
        [Fact]
        public async Task Can_Consume_On_Channel()
        {
            var mockConnectionManager = new Mock<IConnectionManager>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<IChannel>();
            mockConnectionManager.Setup(_ => _.GetConnectionAsync()).ReturnsAsync(connection.Object);
            connection.Setup(_ => _.CreateChannelAsync(null, default)).ReturnsAsync(channel.Object);
            var messageSerializer = new Mock<IMessageSerializer>();

            var mockSubscriberNotifierService = new Mock<ISubscriberNotifierService>();
            var topic = A<Topic>();

            var sut = new RabbitMQConsumer(
                mockConnectionManager.Object,
                mockSubscriberNotifierService.Object,
                messageSerializer.Object,
                topic
            );

            await sut.StartListeningAsync();
            AssertChannelDeclaresQueue(channel);
            AssertChannelConsumes(channel);
        }

        private void AssertChannelDeclaresQueue(Mock<IChannel> channel)
        {
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

        private void AssertChannelConsumes(Mock<IChannel> channel)
        {
            channel.Verify(ch => ch.BasicConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<IAsyncBasicConsumer>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }
    }
}
