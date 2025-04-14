using CSharpFunctionalExtensions;
using PubSub.Core.Abstractions.ErrorTypes;
using PubSub.Core.Models;
using PubSub.DataAccess;

namespace PubSub.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITopicConsumerInstantiatorService _topicConsumerInstantiatorService;
    public SubscriptionService(ISubscriptionRepository repository, ICurrentUserService currentUserService, ITopicConsumerInstantiatorService topicConsumerInstantiatorService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _topicConsumerInstantiatorService = topicConsumerInstantiatorService;
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
                await _topicConsumerInstantiatorService.InstantiateTopic(sub.Topic);
            }
        }
    }

    public Task<IEnumerable<Subscription>> GetActiveUserSubscriptions()
    {
        return _repository.GetAllByUserId(_currentUserService.UserId.Value);
    }

    public async Task<Maybe<OperationError>> DeleteSubscription(Guid uuid)
    {
        var subscriptionMaybe = await _repository.GetAsync(uuid);
        if (subscriptionMaybe.HasNoValue)
        {
            return OperationError.NotFound;
        }
        if (!CanDeleteSubscription(subscriptionMaybe.Value))
        {
            return OperationError.Forbidden;
        }

        await _repository.DeleteAsync(subscriptionMaybe.Value);
        return Maybe<OperationError>.None;
    }

    private bool CanDeleteSubscription(Subscription subscription)
    {
        return subscription.OwnerId == _currentUserService.UserId;
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