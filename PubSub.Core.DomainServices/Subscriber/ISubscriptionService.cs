using CSharpFunctionalExtensions;
using PubSub.Core.Abstractions.ErrorTypes;
using PubSub.Core.DomainModel;
using PubSub.Core.ApplicationServices.Models;

namespace PubSub.Core.DomainServices.Subscriber;

public interface ISubscriptionService
{
    public Task AddSubscriptionsAsync(IEnumerable<CreateSubscriptionParameters> request);
    public Task<IEnumerable<Subscription>> GetActiveUserSubscriptions();
    public Task<Maybe<OperationError>> DeleteSubscription(Guid uuid);
}