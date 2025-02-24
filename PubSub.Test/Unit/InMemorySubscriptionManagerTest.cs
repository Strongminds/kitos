using AutoFixture;
using PubSub.Application;

namespace PubSub.Test.Unit
{
    public class InMemorySubscriptionManagerTest
    {
        private Fixture _fixture;

        public InMemorySubscriptionManagerTest()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public async Task Can_Add_Subscriptions_To_Empty_Collection()
        {
            //todo also assert that adding does not overwrite

            var sut = new InMemorySubscriptionManager();
            var subscriptions = new List<Subscription> { _fixture.Create<Subscription>() };

            await sut.Add(subscriptions);

            var actual = sut.Get();

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
    }
}
