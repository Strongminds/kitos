namespace PubSub.Application
{
    public interface IPublisher
    {
        Task Publish(Publication publication);
    }
}
