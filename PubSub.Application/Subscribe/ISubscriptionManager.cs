using PubSub.Application.Common;

namespace PubSub.Application.Subscribe
{
    public interface ISubscriptionManager
    {
        Dictionary<Topic, ISet<string>> Get();
        Task Add(IEnumerable<Subscription> subscriptions);
    }
}