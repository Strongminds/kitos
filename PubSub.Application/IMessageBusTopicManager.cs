namespace PubSub.Application
{
    public interface IMessageBusTopicManager
    {
        Task Add(Topic topic);
    }
}
