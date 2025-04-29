using PubSub.Core.DomainModel;

namespace PubSub.Application.Services
{
    public interface IPublisherService
    {
        Task PublishAsync(Publication publication);
    }
}
