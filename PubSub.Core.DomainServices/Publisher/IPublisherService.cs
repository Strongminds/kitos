using PubSub.Core.DomainModel;

namespace PubSub.Core.DomainServices.Publisher
{
    public interface IPublisherService
    {
        Task PublishAsync(Publication publication);
    }
}
