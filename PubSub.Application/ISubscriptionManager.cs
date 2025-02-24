namespace PubSub.Application
{
    public interface ISubscriptionManager
    {
        Dictionary<Topic, ISet<string>> Get();
        Task Add(IEnumerable<Subscription> subscriptions);
    }
}