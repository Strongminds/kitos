using AutoFixture;
using Moq;
using PubSub.Core.Consumers;
using PubSub.Core.Managers;
using PubSub.Core.Services.Notifier;
using RabbitMQ.Client;

namespace PubSub.Test.Unit.Core
{
    public class RabbitMQConsumerTest
    {
        [Fact]
        public async Task Can_Consume_On_Channeæ()
        {
            var fixture = new Fixture();
            var mockConnectionManager = new Mock<IConnectionManager>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<IChannel>();
            mockConnectionManager.Setup(_ => _.GetConnectionAsync()).ReturnsAsync(connection.Object);
            connection.Setup(_ => _.CreateChannelAsync(null, default)).ReturnsAsync(channel.Object);

            var mockSubscriberNotifierService = new Mock<ISubscriberNotifierService>();
            var topic = fixture.Create<string>();

            var sut = new RabbitMQConsumer(
                mockConnectionManager.Object,
                mockSubscriberNotifierService.Object,
                topic
            );

            await sut.StartListeningAsync();
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
