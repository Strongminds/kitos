using Moq;
using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Serializer;
using PubSub.Core.DomainServices.Subscriber;
using PubSub.Test.Base.Tests.Toolkit.Patterns;
using PubSub.Application.Services;
using PubSub.Infrastructure.MessageQueue;
using PubSub.Core.DomainModel.Repositories;


namespace PubSub.Test.Unit.Core
{
    public class RabbitMQTopicConsumerInstantiatorServiceTest : WithAutoFixture
    {
        private RabbitMQTopicConsumerInstantiatorService _sut;
        private readonly Mock<ITopicConsumerStore> _subscriptionStore;
        private readonly Mock<IRabbitMQConsumerFactory> _consumerFactory;
        private readonly Mock<IRabbitMQConnectionManager> _mockConnectionManager;
        private readonly Mock<ISubscriberNotifierService> _mockSubscriberNotifierService;
        private readonly Mock<IJsonPayloadSerializer> _messageSerializer;
        private readonly Mock<ISubscriptionRepositoryProvider> _subscriptionRepository;


        public RabbitMQTopicConsumerInstantiatorServiceTest()
        {
            _mockConnectionManager = new Mock<IRabbitMQConnectionManager>();
            _subscriptionStore = new Mock<ITopicConsumerStore>();
            _consumerFactory = new Mock<IRabbitMQConsumerFactory>();
            _mockSubscriberNotifierService = new Mock<ISubscriberNotifierService>();
            _messageSerializer = new Mock<IJsonPayloadSerializer>();
            _subscriptionRepository = new Mock<ISubscriptionRepositoryProvider>();
            _sut = new RabbitMQTopicConsumerInstantiatorService(_mockConnectionManager.Object, _mockSubscriberNotifierService.Object, _subscriptionStore.Object, _consumerFactory.Object, _messageSerializer.Object, _subscriptionRepository.Object);
        }

        [Fact]
        public async Task Can_Add_And_Start_New_Consumer_If_None_Found_For_Topic()
        {
            var topic = A<string>();
            var consumer = new Mock<IConsumer>();
            _subscriptionStore.Setup(x => x.HasConsumer(topic)).Returns(false);
            _consumerFactory.Setup(x => x.Create(It.IsAny<IRabbitMQConnectionManager>(), It.IsAny<ISubscriberNotifierService>(), It.IsAny<IJsonPayloadSerializer>(), It.IsAny<string>(), It.IsAny<ISubscriptionRepositoryProvider>())).Returns(consumer.Object);

            await _sut.InstantiateTopic(topic);

            _subscriptionStore.Verify(x => x.SetConsumerForTopic(topic, consumer.Object));
            consumer.Verify(x => x.StartListeningAsync());
        }
    }
}
