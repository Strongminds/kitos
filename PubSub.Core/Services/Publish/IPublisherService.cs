namespace PubSub.Core.Services.Publish
{
    public interface IPublisherService
    {
        Task Publish(string topic, string message);
    }
}
