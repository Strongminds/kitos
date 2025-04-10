using CSharpFunctionalExtensions;
using PubSub.Core.Models;
using PubSub.DataAccess;
using PubSub.DataAccess.Helpers;

namespace PubSub.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    public SubscriptionService(ISubscriptionRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }
    public async Task AddSubscriptionsAsync(IEnumerable<Subscription> subscriptions)
    {
        var maybeUserId = _currentUserService.UserId;
        var subs = maybeUserId.Map(userId =>
             subscriptions.Select(x => ToSubscriptionWithOwnerId(x, userId))
        );
        if (subs.HasValue)
        {
            await _repository.AddRangeAsync(subs.Value);
        }
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync()
    {
        return await _repository.GetAllAsync();
    }

    private static Subscription ToSubscriptionWithOwnerId(Subscription subscription, string ownerId)
    {
        return new Subscription(subscription.Callback, subscription.Topic.Name)
        {
            Uuid = subscription.Uuid,
            OwnerId = ownerId
        };
    }
}