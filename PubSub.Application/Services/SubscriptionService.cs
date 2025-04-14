using CSharpFunctionalExtensions;
using PubSub.Core.Abstractions.ErrorTypes;
using PubSub.Core.Models;
using PubSub.DataAccess;

namespace PubSub.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriberService _subscriberService;
    public SubscriptionService(ISubscriptionRepository repository, ICurrentUserService currentUserService, ISubscriberService subscriberService)
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
                var exists = await _repository.Exists(sub.Topic, sub.Callback);
                if (exists) continue;
                await _repository.AddAsync(sub);
                await _subscriberService.AddSubscriptionsAsync(sub.Topic);
            }
        }
    }

    public Task<IEnumerable<Subscription>> GetActiveUserSubscriptions()
    {
        return _repository.GetAllByUserId(_currentUserService.UserId.Value);
    }

    public async Task<Maybe<OperationError>> DeleteSubscription(Guid uuid)
    {
        var subscription = await _repository.GetAsync(uuid);
        return subscription.Match(ValidateUserOwnsSubscription, () => OperationError.NotFound);
    }

    private Maybe<OperationError> ValidateUserOwnsSubscription(Subscription subscription)
    {
        if (subscription.OwnerId != _currentUserService.UserId)
        {
            return OperationError.Forbidden;
        }
        return Maybe<OperationError>.None;
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