using System.Text;
using PubSub.Core.Managers;
using PubSub.Core.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PubSub.Core.Consumers
{
    public class RabbitMQConsumer : IDisposable
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ISubscriberNotifierService _subscriberNotifierService;
        private readonly string _queueName;
        private readonly List<string> _callbackUrls = new();
        private IConnection _connection;
        private IChannel _channel;
        private AsyncEventingBasicConsumer _consumer;


        public RabbitMQConsumer(IConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, string queueName)
        {
            _connectionManager = connectionManager;
            _subscriberNotifierService = subscriberNotifierService;
            _queueName = queueName;
        }

        public async Task StartListeningAsync()
        {
            _connection = await _connectionManager.GetConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(_queueName, true, false, false);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                foreach (var callbackUrl in _callbackUrls)
                {
                    await _subscriberNotifierService.Notify(message, callbackUrl);
                }
                Console.WriteLine($"Received {message}");
            };

            await _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer: _consumer);
        }

        public void AddCallbackUrl(string callbackUrl)
        {
            _callbackUrls.Add(callbackUrl);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
