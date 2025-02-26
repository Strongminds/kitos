using AutoFixture;
using Moq;
using PubSub.Core.Consumers;
using PubSub.Core.Services.Subscribe;

namespace PubSub.Test.Unit.Core
{
    public class InMemorySubscriptionStoreTest
    {
        private InMemorySubscriptionStore _sut;
        private IFixture _fixture;

        public InMemorySubscriptionStoreTest()
        {
            _fixture = new Fixture();
            _sut = new InMemorySubscriptionStore();
        }

        [Fact]
        public void Can_Add_New_Topic()
        {
            var topic = _fixture.Create<string>();
            var consumer = new Mock<IConsumer>();

            _sut.SetConsumerForTopic(topic, consumer.Object);

            var subscriptions = _sut.GetSubscriptions();
            Assert.Equal(subscriptions[topic],  consumer.Object);
        }

        [Fact]
        public void Updates_Consumer_If_Topic_Exists()
        {
            var topic = _fixture.Create<string>();
            var consumer = new Mock<IConsumer>();
            _sut.SetConsumerForTopic(topic, consumer.Object);
            var secondConsumer = new Mock<IConsumer>();

            _sut.SetConsumerForTopic(topic, secondConsumer.Object);

            var subscriptions = _sut.GetSubscriptions();
            Assert.Equal(subscriptions[topic], secondConsumer.Object);
        }

        [Fact]
        public void Can_Add_Callback_To_Consumer_By_Topic()
        {
            var (topic, callback, consumer) = SetupCreateConsumer();

            _sut.AddCallbackToTopic(topic,callback);

            consumer.Verify(_ => _.AddCallbackUrl(callback));
        }

        private (string topic, string callback, Mock<IConsumer> consumer) SetupCreateConsumer()
        {
            var topic = _fixture.Create<string>();
            var consumer = new Mock<IConsumer>();
            var callback = _fixture.Create<string>();
            _sut.SetConsumerForTopic(topic, consumer.Object);
            return (topic, callback, consumer);
        }
    }
}
