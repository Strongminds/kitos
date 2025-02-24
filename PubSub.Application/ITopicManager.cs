namespace PubSub.Application
{
    public interface ITopicManager
    {
        Dictionary<Topic, IEnumerable<string>> Get();
        Task Add(IEnumerable<Subscription> subscriptions);
    }
}