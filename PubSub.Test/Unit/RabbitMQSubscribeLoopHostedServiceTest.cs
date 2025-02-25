using AutoFixture;
using PubSub.Application;
using Moq;
using RabbitMQ.Client;

namespace PubSub.Test.Unit
{
    public class RabbitMQSubscribeLoopHostedServiceTest
    {
        private Fixture _fixture;
        private Mock<IMessageSerializer> _messageSerializer;
        private Mock<IChannel> _channel;
        private Mock<ISubscriptionManager> _topicManager;
        private RabbitMQSubscribeLoopHostedService _sut;

        public RabbitMQSubscribeLoopHostedServiceTest()
        {
            _fixture = new Fixture();
            _messageSerializer = new Mock<IMessageSerializer>();
            _channel = new Mock<IChannel>();
            _topicManager = new Mock<ISubscriptionManager>();
            _sut = new RabbitMQSubscribeLoopHostedService(_messageSerializer.Object, _channel.Object, _topicManager.Object);
        }

        [Fact]
        public async Task Can_Update_Subscriptions()
        {
            var subscriptions = new List<Subscription> { _fixture.Create<Subscription>() };

            await _sut.UpdateSubscriptions(subscriptions);

            _topicManager.Verify(_ => _.Add(subscriptions));
        }
    }
}
