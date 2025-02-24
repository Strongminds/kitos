﻿
namespace PubSub.Application
{
    public class InMemorySubscriptionManager : ISubscriptionManager
    {
        private readonly Dictionary<Topic, ISet<string>> topics = new Dictionary<Topic, ISet<string>>();

        public async Task Add(IEnumerable<Subscription> subscriptions)
        {
            foreach (var subscription in subscriptions)
            {
                foreach (var topic in subscription.Queues)
                {
                    topics.Add(new Topic { Name = topic }, new HashSet<string>() { subscription.Callback });
                }
            }
        }

        public Dictionary<Topic, ISet<string>> Get()
        {
            return topics;
        }
    }
}
