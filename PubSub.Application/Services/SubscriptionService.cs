using PubSub.Core.Models;
using PubSub.DataAccess;

namespace PubSub.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    public SubscriptionService(ISubscriptionRepository repository)
    {
        _repository = repository;
    }
    public async Task AddSubscriptionsAsync(IEnumerable<Subscription> subscriptions)
    {
        await _repository.AddRangeAsync(subscriptions);
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync()
    {
        return await _repository.GetAllAsync();
    }
}