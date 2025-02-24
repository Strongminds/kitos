using AutoFixture;
using PubSub.Application;

namespace PubSub.Test.Unit
{
    public class InMemorySubscriptionManagerTest
    {
        private Fixture _fixture;
        private InMemorySubscriptionManager _sut;

        public InMemorySubscriptionManagerTest()
        {
            _fixture = new Fixture();
            _sut = new InMemorySubscriptionManager();
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
            foreach (var queue in expected.Queues)
            {
                var asTopic = new Topic() { Name = queue };
                if (actual.TryGetValue(asTopic, out var callbacks))
                {
                    Assert.NotEmpty(callbacks);
                    Assert.Contains(expected.Callback, callbacks);
                }
            }
        }

        [Fact]
        public async Task Can_Add_Subscription_To_Existing_Topic()
        {
            var setupSubscription = _fixture.Create<Subscription>();
            var topic = new Topic() { Name = setupSubscription.Queues.First() };
            Assert.NotNull(topic);
            var setupSubs = new List<Subscription> { setupSubscription };
            await _sut.Add(setupSubs);

            var newSub = new Subscription()
            {
                Queues = new List<string> { topic.Name },
                Callback = _fixture.Create<string>()
            };

            await _sut.Add(new List<Subscription> { newSub });

            var actual = _sut.Get();
            var actualCallbacks = actual.First(x => x.Key == topic).Value;
            Assert.Equal(2, actualCallbacks.Count);

        }
    }
}
