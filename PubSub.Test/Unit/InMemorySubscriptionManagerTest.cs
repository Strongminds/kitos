using AutoFixture;
using Moq;
using PubSub.Application;

namespace PubSub.Test.Unit
{
    public class InMemorySubscriptionManagerTest
    {
        private Fixture _fixture;
        private Mock<IMessageBusTopicManager> _messageBusTopicManager;
        private InMemorySubscriptionManager _sut;

        public InMemorySubscriptionManagerTest()
        {
            _fixture = new Fixture();
            _messageBusTopicManager = new Mock<IMessageBusTopicManager>();
            _sut = new InMemorySubscriptionManager(_messageBusTopicManager.Object);
        }

        [Fact]
        public async Task Can_Add_Subscriptions_With_New_Topic()
        {
            var subscriptions = new List<Subscription> { _fixture.Create<Subscription>() };
            var existing = _sut.Get();
            Assert.Empty(existing);

            await _sut.Add(subscriptions);

            var actual = _sut.Get();
            var expected = subscriptions.First();
            foreach (var topic in expected.Topics)
            {
                if (actual.TryGetValue(topic, out var callbacks))
                {
                    Assert.NotEmpty(callbacks);
                    Assert.Contains(expected.Callback, callbacks);
                }
            }
            _messageBusTopicManager.Verify(_ => _.Add(It.IsAny<Topic>()));
        }

        [Fact]
        public async Task Can_Add_Subscription_To_Existing_Topic()
        {
            var setupSubscription = _fixture.Create<Subscription>();
            var topic = setupSubscription.Topics.First();
            Assert.NotNull(topic);
            var setupSubs = new List<Subscription> { setupSubscription };
            await _sut.Add(setupSubs);

            var newSub = new Subscription()
            {
                Topics = new List<Topic> { topic },
                Callback = _fixture.Create<string>()
            };

            await _sut.Add(new List<Subscription> { newSub });

            var actual = _sut.Get();
            var actualCallbacks = actual.First(x => x.Key == topic).Value;
            Assert.Equal(2, actualCallbacks.Count);
            _messageBusTopicManager.Verify(_ => _.Add(It.IsAny<Topic>()));
        }
    }
}
