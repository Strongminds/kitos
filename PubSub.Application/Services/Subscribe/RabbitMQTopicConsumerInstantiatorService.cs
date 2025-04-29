using PubSub.Application.Services.RabbitMQConnection;
using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Serializer;

namespace PubSub.Application.Services.Consumer
{
    public class RabbitMQTopicConsumerInstantiatorService : ITopicConsumerInstantiatorService
    {
        private readonly IRabbitMQConnectionManager _connectionManager;
        private readonly ISubscriberNotifierService _subscriberNotifierService;
        private readonly ITopicConsumerStore _topicConsumerStore;
        private readonly IRabbitMQConsumerFactory _consumerFactory;
        private readonly IPayloadSerializer _payloadSerializer;
        private readonly IServiceScopeFactory _scopeFactory;

        public RabbitMQTopicConsumerInstantiatorService(IRabbitMQConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, ITopicConsumerStore topicConsumerStore, IRabbitMQConsumerFactory consumerFactory, IPayloadSerializer payloadSerializer, IServiceScopeFactory scopeFactory)
        {
            _connectionManager = connectionManager;
            _subscriberNotifierService = subscriberNotifierService;
            _topicConsumerStore = topicConsumerStore;
            _consumerFactory = consumerFactory;
            _payloadSerializer = payloadSerializer;
            _scopeFactory = scopeFactory;
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
            var consumer = _consumerFactory.Create(_connectionManager, _subscriberNotifierService, _payloadSerializer, topic, _scopeFactory);
            _topicConsumerStore.SetConsumerForTopic(topic, consumer);
            await consumer.StartListeningAsync();
        }
    }
}
