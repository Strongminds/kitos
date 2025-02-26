using AutoFixture;
using Moq;
using PubSub.Core.Consumers;
using PubSub.Core.Managers;
using PubSub.Core.Models;
using PubSub.Core.Services.Notifier;
using PubSub.Core.Services.Subscribe;

namespace PubSub.Test.Unit.Core
{
    public class TestRabbitMQConsumer : RabbitMQConsumer
    {
        public TestRabbitMQConsumer(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, string topic)
            : base(connectionManager, subscriberNotifierService, topic) { }

        public override Task StartListeningAsync() => Task.CompletedTask;
    }
    public class RabbitMQSubscriberServiceTest
    {
        private RabbitMQSubscriberService _sut;
        private readonly Fixture _fixture;
        private readonly Mock<ISubscriptionStore> _subscriptionStore;
        private readonly Mock<IRabbitMQConsumerFactory> _consumerFactory;
        private readonly Mock<IConnectionManager> _mockConnectionManager;
        private readonly Mock<ISubscriberNotifierService> _mockSubscriberNotifierService;

        public RabbitMQSubscriberServiceTest()
        {
            _fixture = new Fixture();
            _mockConnectionManager = new Mock<IConnectionManager>();
            _subscriptionStore = new Mock<ISubscriptionStore>();
            _consumerFactory = new Mock<IRabbitMQConsumerFactory>();
            _mockSubscriberNotifierService = new Mock<ISubscriberNotifierService>();
            _sut = new RabbitMQSubscriberService(_mockConnectionManager.Object, _mockSubscriberNotifierService.Object, _subscriptionStore.Object, _consumerFactory.Object);
        }

        [Fact]
        public async Task Can_Add_And_Start_New_Consumer_If_None_Found_For_Topic()
        {
            var subscription = _fixture.Create<Subscription>();
            var subs = new List<Subscription> { subscription };
            var mockConnectionManager = new Mock<IConnectionManager>();
            var mockSubscriberNotifierService = new Mock<ISubscriberNotifierService>();
            string topic = subscription.Topics.First();

            var consumer = new Mock<TestRabbitMQConsumer>(
                mockConnectionManager.Object,
                mockSubscriberNotifierService.Object,
                topic
            );

            _subscriptionStore.Setup(_ => _.GetSubscriptions()).Returns(new Dictionary<string, IConsumer>());
            _consumerFactory.Setup(_ => _.Create(It.IsAny<IConnectionManager>(), It.IsAny<ISubscriberNotifierService>(), It.IsAny<string>())).Returns(consumer.Object);

            await _sut.AddSubscriptionsAsync(subs);
            _consumerFactory.Verify(_ => _.Create(It.IsAny<IConnectionManager>(), It.IsAny<ISubscriberNotifierService>(), It.IsAny<string>()));
            consumer.Verify(_ => _.StartListeningAsync());
        }
    }
}
