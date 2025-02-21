namespace PubSub.Application
{
    public interface IPublisher
    {
        Task Publish(string queue, string message);
    }
}
