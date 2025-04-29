using PubSub.Core.DomainModel;

namespace PubSub.Application.Services.Publisher
{
    public interface IPublisherService
    {
        Task PublishAsync(Publication publication);
    }
}
