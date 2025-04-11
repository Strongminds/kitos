using PubSub.Core.Models;

namespace PubSub.Application.Services
{
    public interface ISubscriberService
    {
        Task AddSubscriptionsAsync(IEnumerable<Subscription> subscriptions);
    }
}
