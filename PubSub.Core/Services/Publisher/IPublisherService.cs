using PubSub.Core.Models;

namespace PubSub.Core.Services.Publish
{
    public interface IPublisherService
    {
        Task Publish(Publication publication);
    }
}
