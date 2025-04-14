using CSharpFunctionalExtensions;
using PubSub.Core.Abstractions.ErrorTypes;
using PubSub.Core.Models;

namespace PubSub.Application.Services;

public interface ISubscriptionService
{
    public Task AddSubscriptionsAsync(IEnumerable<Subscription> subscriptions);
    public Task<IEnumerable<Subscription>> GetActiveUserSubscriptions();

    public Task<Maybe<OperationError>> DeleteSubscription(Guid uuid);
}