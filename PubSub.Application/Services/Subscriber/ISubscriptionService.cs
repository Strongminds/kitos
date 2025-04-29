using CSharpFunctionalExtensions;
using PubSub.Application.Models;
using PubSub.Core.Abstractions.ErrorTypes;
using PubSub.Core.DomainModel;

namespace PubSub.Application.Services.Subscribe;

public interface ISubscriptionService
{
    public Task AddSubscriptionsAsync(IEnumerable<CreateSubscriptionParameters> request);
    public Task<IEnumerable<Subscription>> GetActiveUserSubscriptions();
    public Task<Maybe<OperationError>> DeleteSubscription(Guid uuid);
}