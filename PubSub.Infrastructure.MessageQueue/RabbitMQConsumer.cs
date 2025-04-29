using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Repositories;
using PubSub.Core.ApplicationServices.Serializer;
using PubSub.Application.Services;
using PubSub.Core.DomainServices.Subscriber;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PubSub.Infrastructure.MessageQueue
{
    public class RabbitMQConsumer : IConsumer
    {
        private readonly IRabbitMQConnectionManager _connectionManager;
        private readonly ISubscriberNotifierService _subscriberNotifierService;
        private readonly string _topic;
        private readonly IPayloadSerializer _payloadSerializer;
        private IConnection _connection;
        private IChannel _channel;
        private IAsyncBasicConsumer _consumerCallback;
        private readonly ISubscriptionRepositoryProvider subscriptionRepositoryProvider;

        public RabbitMQConsumer(IRabbitMQConnectionManager connectionManager, 
            ISubscriberNotifierService subscriberNotifierService, 
            IPayloadSerializer payloadSerializer, 
            string topic,
            ISubscriptionRepositoryProvider subscriptionRepositoryProvider)
        {
            _connectionManager = connectionManager;
            _subscriberNotifierService = subscriberNotifierService;
            _topic = topic;
            _payloadSerializer = payloadSerializer;
            this.subscriptionRepositoryProvider = subscriptionRepositoryProvider;
        }

        public async Task StartListeningAsync()
        {
            var topicName = _topic;
            _connection = await _connectionManager.GetConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.QueueDeclareAsync(topicName, true, false, false);
            _consumerCallback = GetConsumerCallback();

            await _channel.BasicConsumeAsync(topicName, autoAck: true, consumer: _consumerCallback);
        }

        private AsyncEventingBasicConsumer GetConsumerCallback()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var payload = _payloadSerializer.Deserialize(body);
                    var repository = subscriptionRepositoryProvider.Get();
                    var subscriptions = await repository.GetByTopic(_topic);
                    foreach (var callbackUrl in subscriptions.Select(x => x.Callback))
                    {
                        await _subscriberNotifierService.Notify(payload, callbackUrl);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in consumer: {ex.Message}\n{ex.StackTrace}");
                }
            };
            return consumer;
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
