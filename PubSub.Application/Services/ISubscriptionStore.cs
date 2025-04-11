using PubSub.Core.Models;

namespace PubSub.Application.Services
{
    public interface ISubscriptionStore
    {
        void AddCallbackToTopic(string topic, Uri callback);

        void SetConsumerForTopic(string topic, IConsumer consumer);

        IDictionary<string, IConsumer> GetSubscriptions();

        bool HasTopic(string topic);
    }
}
