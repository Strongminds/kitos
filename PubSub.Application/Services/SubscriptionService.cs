using CSharpFunctionalExtensions;
using PubSub.Core.Abstractions.ErrorTypes;
using PubSub.Core.Models;
using PubSub.DataAccess;

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
             subscriptions.Select(x => ToSubscriptionWithOwnerId(x, userId)).ToList()
        );
        if (subs.HasValue)
        {
            foreach (var sub in subs.Value)
            {
                var exists = await _repository.Exists(sub.Topic, sub.Callback);
                if (exists) continue;
                await _repository.AddAsync(sub);
            }
        }
    }

    public Task<IEnumerable<Subscription>> GetActiveUserSubscriptions()
    {
        return _repository.GetAllByUserId(_currentUserService.UserId.Value);
    }

    public async Task<Maybe<OperationError>> DeleteSubscription(Guid uuid)
    {
        await _repository.DeleteAsync(uuid);
        return Maybe.None; //TODO
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