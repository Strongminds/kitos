using CSharpFunctionalExtensions;
using PubSub.Core.Abstractions.ErrorTypes;
using PubSub.Core.Models;

namespace PubSub.DataAccess;

public interface ISubscriptionRepository
{
    Task<IEnumerable<Subscription>> GetAllByUserId(string userId);
    Task<IEnumerable<Subscription>> GetByTopic(string topic);
    Task<bool> Exists(string topic, string url);
    Task<Maybe<Subscription>> GetAsync(Guid uuid);
    Task AddAsync(Subscription subscription);
    Task AddRangeAsync(IEnumerable<Subscription> subscriptions);
    Task UpdateAsync(Subscription subscription);
    Task<Maybe<OperationError>> DeleteAsync(Guid uuid);
}