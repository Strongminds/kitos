namespace PubSub.Application.Publish
{
    public interface IPublisher
    {
        Task Publish(Publication publication);
    }
}
