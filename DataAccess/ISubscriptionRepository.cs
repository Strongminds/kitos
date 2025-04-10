using CSharpFunctionalExtensions;
using PubSub.Core.Models;

namespace PubSub.DataAccess;

public interface ISubscriptionRepository
{
    Task<IEnumerable<Subscription>> GetAllAsync();
    Task<IEnumerable<Subscription>> GetByTopic(string topic);
    Task<Maybe<Subscription>> GetAsync(Guid uuid);
    Task AddAsync(Subscription subscription);
    Task AddRangeAsync(IEnumerable<Subscription> subscriptions);
    Task UpdateAsync(Subscription subscription);
    Task DeleteAsync(Guid uuid);
}