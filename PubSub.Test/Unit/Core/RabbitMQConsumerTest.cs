using Moq;
using PubSub.Core.ApplicationServices.Notifier;
using RabbitMQ.Client;
using PubSub.Test.Base;
using PubSub.Application.Services;
using PubSub.Infrastructure.MessageQueue;
using PubSub.Core.DomainModel.Repositories;
using PubSub.Core.DomainModel.Serializer;

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
            var repo = new Mock<ISubscriptionRepositoryProvider>();
            mockConnectionManager.Setup(_ => _.GetConnectionAsync()).ReturnsAsync(connection.Object);
            connection.Setup(_ => _.CreateChannelAsync(null, default)).ReturnsAsync(channel.Object);
            var messageSerializer = new Mock<IJsonPayloadSerializer>();

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
