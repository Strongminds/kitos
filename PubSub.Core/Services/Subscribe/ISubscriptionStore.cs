using PubSub.Core.Consumers;

namespace PubSub.Core.Services.Subscribe
{
    public interface ISubscriptionStore
    {
        void AddCallbackToTopic(string topic, string callback);

        void SetConsumerForTopic(string topic, IConsumer consumer);

        IDictionary<string, IConsumer> GetSubscriptions();
    }
}
