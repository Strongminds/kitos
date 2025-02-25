using PubSub.Application.Common;

namespace PubSub.Application.Subscribe
{
    public class InMemorySubscriptionManager : ISubscriptionManager
    {
        private readonly Dictionary<Topic, ISet<string>> topics = new Dictionary<Topic, ISet<string>>();
        private readonly IMessageBusTopicManager _messageBusTopicManager;

        public InMemorySubscriptionManager(IMessageBusTopicManager messageBusTopicManager)
        {
            _messageBusTopicManager = messageBusTopicManager;
        }

        public async Task Add(IEnumerable<Subscription> subscriptions)
        {
            foreach (var subscription in subscriptions)
            {
                foreach (var topic in subscription.Topics)
                {
                    if (topics.TryGetValue(topic, out var callbacks)){
                        callbacks.Add(subscription.Callback);
                    }
                    else
                    {
                        topics.Add(topic, new HashSet<string>() { subscription.Callback });
                    }
                    await _messageBusTopicManager.Add(topic);
                }
            }
        }

        public Dictionary<Topic, ISet<string>> Get()
        {
            return topics;
        }
    }
}
