using PubSub.Core.Models;

namespace PubSub.Core.Services.Subscribe
{
    public interface ISubscriberService
    {
        Task SubscribeToQueuesAsync(IEnumerable<Subscription> subscriptions);
    }
}
