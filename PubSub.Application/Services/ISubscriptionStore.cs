using PubSub.Core.Models;

namespace PubSub.Application.Services
{
    public interface ISubscriptionStore
    {
        void AddCallbackToTopic(Topic topic, Uri callback);

        void SetConsumerForTopic(Topic topic, IConsumer consumer);

        IDictionary<Topic, IConsumer> GetSubscriptions();

        bool HasTopic(Topic topic);
    }
}
