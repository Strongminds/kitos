using PubSub.Application.Common;
using RabbitMQ.Client;

namespace PubSub.Application.Subscribe
{
    public class InMemorySubscriptionManager : ISubscriptionManager
    {
        private readonly Dictionary<string, ISet<string>> topics = new Dictionary<string, ISet<string>>();
        private readonly IChannel channel;
        private readonly object _lock = new();


        public InMemorySubscriptionManager(IChannel channel)
        {
            this.channel = channel;
        }

        public async Task Add(IEnumerable<Subscription> subscriptions)
        {
            var newQueues = new HashSet<string>();
            lock (_lock)
            {
                foreach (var subscription in subscriptions)
                {
                    foreach (var topic in subscription.Topics)
                    {
                        if (topics.TryGetValue(topic, out var callbacks))
                        {
                            callbacks.Add(subscription.Callback);
                        }
                        else
                        {
                            topics[topic] = new HashSet<string> { subscription.Callback };
                            newQueues.Add(topic);
                        }
                    }
                }
            }

            foreach (var topic in newQueues)
            {
                await channel.QueueDeclareAsync(topic);
            }
        }

        public Dictionary<string, HashSet<string>> Get()
        {
           return topics.ToDictionary(kvp => kvp.Key, kvp => new HashSet<string>(kvp.Value));
        }
    }
}
