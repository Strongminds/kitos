namespace PubSub.Core.DomainServices.Subscriber
{
    public interface ITopicConsumerStore
    {
        void SetConsumerForTopic(string topic, IConsumer consumer);

        bool HasConsumer(string topic);
    }
}
