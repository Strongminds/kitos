using CSharpFunctionalExtensions;
using PubSub.Core.Models;
using PubSub.DataAccess;

namespace PubSub.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriberService _subscriberService;
    public SubscriptionService(ISubscriptionRepository repository, ISubscriberService subscriberService, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _subscriberService = subscriberService;
    }
    public async Task AddSubscriptionsAsync(IEnumerable<Subscription> subscriptions)
    {
        var maybeUserId = _currentUserService.UserId;
        var subs = maybeUserId.Map(userId =>
             subscriptions.Select(x => ToSubscriptionWithOwnerId(x, userId)).ToList()
        );
        if (subs.HasValue)
        {
            foreach (var sub in subs.Value)
            {
                var exists = await _repository.Exists(sub.Topic, sub.Callback.AbsoluteUri);
                if (exists) continue;
                await _repository.AddAsync(sub);
            }

            await _subscriberService.AddSubscriptionsAsync(subs.Value);
        }
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync()
    {
        return await _repository.GetAllAsync();
    }

    private static Subscription ToSubscriptionWithOwnerId(Subscription subscription, string ownerId)
    {
        return new Subscription(subscription.Callback, subscription.Topic)
        {
            Uuid = subscription.Uuid,
            OwnerId = ownerId
        };
    }
}