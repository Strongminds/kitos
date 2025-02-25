using PubSub.Application.Common;

namespace PubSub.Application.Subscribe
{
    public interface ISubscriptionManager
    {
        Dictionary<string, ISet<string>> Get();
        Task Add(IEnumerable<Subscription> subscriptions);
    }
}