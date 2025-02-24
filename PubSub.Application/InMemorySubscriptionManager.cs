
namespace PubSub.Application
{
    public class InMemorySubscriptionManager : ISubscriptionManager
    {
        private readonly Dictionary<Topic, ISet<string>> topics = new Dictionary<Topic, ISet<string>>();

        public async Task Add(IEnumerable<Subscription> subscriptions)
        {
            foreach (var subscription in subscriptions)
            {
                foreach (var topic in subscription.Topics)
                {
                    var asTopic = new Topic { Name = topic };
                    if (topics.TryGetValue(asTopic, out var callbacks)){
                        callbacks.Add(subscription.Callback);
                    }
                    else
                    {
                        topics.Add(asTopic, new HashSet<string>() { subscription.Callback });
                    }
                }
            }
        }

        public Dictionary<Topic, ISet<string>> Get()
        {
            return topics;
        }
    }
}
