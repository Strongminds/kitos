using PubSub.Core.Models;

namespace PubSub.Core.Services
{
    public interface ISubscriberService
    {
        Task SubscribeToQueuesAsync(IEnumerable<Subscription> subscriptions);
    }
}
