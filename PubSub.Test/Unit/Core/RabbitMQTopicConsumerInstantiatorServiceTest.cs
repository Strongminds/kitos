﻿using Microsoft.Extensions.DependencyInjection;
using Moq;
using PubSub.Application.Services;
using PubSub.Core.Models;
using PubSub.Core.Services.Notifier;
using PubSub.Core.Services.Serializer;
using PubSub.DataAccess;
using PubSub.Test.Base.Tests.Toolkit.Patterns;

namespace PubSub.Test.Unit.Core
{
    public class RabbitMQTopicConsumerInstantiatorServiceTest : WithAutoFixture
    {
        private RabbitMQTopicConsumerInstantiatorService _sut;
        private readonly Mock<ISubscriptionStore> _subscriptionStore;
        private readonly Mock<IRabbitMQConsumerFactory> _consumerFactory;
        private readonly Mock<IConnectionManager> _mockConnectionManager;
        private readonly Mock<ISubscriberNotifierService> _mockSubscriberNotifierService;
        private readonly Mock<IPayloadSerializer> _messageSerializer;
        private readonly Mock<IServiceScopeFactory> _subscriptionRepository;
        

        public RabbitMQTopicConsumerInstantiatorServiceTest()
        {
            _mockConnectionManager = new Mock<IConnectionManager>();
            _subscriptionStore = new Mock<ISubscriptionStore>();
            _consumerFactory = new Mock<IRabbitMQConsumerFactory>();
            _mockSubscriberNotifierService = new Mock<ISubscriberNotifierService>();
            _messageSerializer = new Mock<IPayloadSerializer>();
            _subscriptionRepository = new Mock<IServiceScopeFactory>();
            _sut = new RabbitMQTopicConsumerInstantiatorService(_mockConnectionManager.Object, _mockSubscriberNotifierService.Object, _subscriptionStore.Object, _consumerFactory.Object, _messageSerializer.Object, _subscriptionRepository.Object);
        }

        [Fact]
        public async Task Can_Add_And_Start_New_Consumer_If_None_Found_For_Topic()
        {
            var subscription = A<Subscription>();
            var subs = new List<Subscription> { subscription };
            var mockConnectionManager = new Mock<IConnectionManager>();
            var mockSubscriberNotifierService = new Mock<ISubscriberNotifierService>();
            var topic = subscription.Topic;
            var consumer = new Mock<IConsumer>();

            _subscriptionStore.Setup(_ => _.GetSubscriptions()).Returns(new Dictionary<string, IConsumer>());
            _consumerFactory.Setup(_ => _.Create(It.IsAny<IConnectionManager>(), It.IsAny<ISubscriberNotifierService>(), It.IsAny<IPayloadSerializer>(), It.IsAny<string>(), It.IsAny<IServiceScopeFactory>())).Returns(consumer.Object);

            //await _sut.AddSubscriptionsAsync(subs);
            _consumerFactory.Verify(_ => _.Create(It.IsAny<IConnectionManager>(), It.IsAny<ISubscriberNotifierService>(), It.IsAny<IPayloadSerializer>(), It.IsAny<string>(), It.IsAny<IServiceScopeFactory>()));
            consumer.Verify(_ => _.StartListeningAsync());
        }
    }
}
