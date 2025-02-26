using AutoFixture;
using PubSub.Core.Consumers;
using PubSub.Core.Services.Subscribe;

namespace PubSub.Test.Unit.Core
{
    public class InMemorySubscriptionStoreTest
    {
        private class TestConsumer : IConsumer
        {
            public void Dispose()
            {
            }

            public Task StartListeningAsync()
            {
                return Task.CompletedTask;
            }
        }

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
            var consumer = new TestConsumer();

            _sut.SetConsumerForTopic(topic, consumer);

            var subscriptions = _sut.GetSubscriptions();
            Assert.Equal(subscriptions[topic],  consumer);
        }

        [Fact]
        public void Updates_Consumer_If_Topic_Exists()
        {
            var topic = _fixture.Create<string>();
            var consumer = new TestConsumer();
            _sut.SetConsumerForTopic(topic, consumer);
            var secondConsumer = new TestConsumer();

            _sut.SetConsumerForTopic(topic, secondConsumer);

            var subscriptions = _sut.GetSubscriptions();
            Assert.Equal(subscriptions[topic], secondConsumer);
        }
    }
}
