namespace PubSub.Application.Services.Consumer
{
    public interface ITopicConsumerStore
    {
        void SetConsumerForTopic(string topic, IConsumer consumer);

        bool HasConsumer(string topic);
    }
}
