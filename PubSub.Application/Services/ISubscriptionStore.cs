namespace PubSub.Application.Services
{
    public interface ISubscriptionStore
    {

        void SetConsumerForTopic(string topic, IConsumer consumer);

        IDictionary<string, IConsumer> GetSubscriptions();

        bool HasTopic(string topic);
    }
}
