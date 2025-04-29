using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Serializer;
using PubSub.Core.DomainServices.Subscriber;
using PubSub.Application.Services;
using PubSub.Core.DomainModel.Repositories;

namespace PubSub.Infrastructure.MessageQueue
{
    public class RabbitMQTopicConsumerInstantiatorService : ITopicConsumerInstantiatorService
    {
        private readonly IRabbitMQConnectionManager _connectionManager;
        private readonly ISubscriberNotifierService _subscriberNotifierService;
        private readonly ITopicConsumerStore _topicConsumerStore;
        private readonly IRabbitMQConsumerFactory _consumerFactory;
        private readonly IJsonPayloadSerializer _payloadSerializer;
        private readonly ISubscriptionRepositoryProvider subscriptionRepositoryProvider;

        public RabbitMQTopicConsumerInstantiatorService(IRabbitMQConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, ITopicConsumerStore topicConsumerStore, IRabbitMQConsumerFactory consumerFactory, IJsonPayloadSerializer payloadSerializer, ISubscriptionRepositoryProvider subscriptionRepositoryProvider)
        {
            _connectionManager = connectionManager;
            _subscriberNotifierService = subscriberNotifierService;
            _topicConsumerStore = topicConsumerStore;
            _consumerFactory = consumerFactory;
            _payloadSerializer = payloadSerializer;
            this.subscriptionRepositoryProvider = subscriptionRepositoryProvider;
        }

        public async Task InstantiateTopic(string topic)
        {
            if (!_topicConsumerStore.HasConsumer(topic))
            {
                await CreateAndStartNewConsumerAsync(topic);
            }
        }

        private async Task CreateAndStartNewConsumerAsync(string topic)
        {
            var consumer = _consumerFactory.Create(_connectionManager, _subscriberNotifierService, _payloadSerializer, topic, subscriptionRepositoryProvider);
            _topicConsumerStore.SetConsumerForTopic(topic, consumer);
            await consumer.StartListeningAsync();
        }
    }
}
