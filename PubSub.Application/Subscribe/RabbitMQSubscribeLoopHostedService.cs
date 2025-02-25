﻿using PubSub.Application.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PubSub.Application.Subscribe
{
    public class RabbitMQSubscribeLoopHostedService : BackgroundService, ISubscribeLoopHostedService
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly IChannel _channel;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly Dictionary<string, ISet<string>> topics = new Dictionary<string, ISet<string>>();


        public RabbitMQSubscribeLoopHostedService(IMessageSerializer messageSerializer, IChannel channel, ISubscriptionManager topicManager)
        {
            _messageSerializer = messageSerializer;
            _channel = channel;
            _subscriptionManager = topicManager;
        }

        public async Task UpdateSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            //await _subscriptionManager.Add(subscriptions);
            var queuesSet = new HashSet<string>();

            foreach (var sub in subscriptions)
            {
                var callback = sub.Callback;
                foreach (var topic in sub.Topics)
                {
                    if (topics.TryGetValue(topic, out var callbacks))
                    {
                        callbacks.Add(callback);
                    }
                    else
                    {
                        topics.Add(topic, new HashSet<string>() { callback });
                    }
                    queuesSet.Add(topic);
                }
            }

            foreach (var queue in queuesSet)
            {
                await _channel.QueueDeclareAsync(queue);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => RunSubscribeLoop(stoppingToken), stoppingToken);
        }

        private async Task RunSubscribeLoop(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ConsumeNextMessage(_channel);
            }
        }

        private async Task ConsumeNextMessage(IChannel channel)
        {
            var subscriptions = _subscriptionManager.Get();
            foreach (var topic in subscriptions.Keys)
            {
                var callbackUrls = subscriptions.GetValueOrDefault(topic);
                foreach (var url in callbackUrls!)
                {
                    var consumerCallback = GetConsumerCallback(channel, url);
                    await channel.BasicConsumeAsync(topic, autoAck: true, consumer: consumerCallback);
                }
            }
        }

        private IAsyncBasicConsumer GetConsumerCallback(IChannel channel, string callbackUrl) {
            var consumerCallback = new AsyncEventingBasicConsumer(channel);
            consumerCallback.ReceivedAsync += async (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = _messageSerializer.Deserialize(body);
                var json = JsonContent.Create(message);
                var content = new StringContent($"\"{message}\"", Encoding.UTF8, "application/json");
                var http = new HttpClient();
                var res = await http.PostAsync(callbackUrl, content);
                Console.WriteLine($"Received {message}");
            };
            return consumerCallback;
        }
    }
}
