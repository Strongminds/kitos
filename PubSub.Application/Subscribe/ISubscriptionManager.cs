using PubSub.Core.Models;

namespace PubSub.Application.Subscribe
{
    public interface ISubscriptionManager
    {
        public Dictionary<string, HashSet<string>> Get();
        Task Add(IEnumerable<Subscription> subscriptions);
    }
}