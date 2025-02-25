using PubSub.Application.Common;

namespace PubSub.Application
{
    public interface IMessageBusTopicManager
    {
        Task Add(Topic topic);
    }
}
