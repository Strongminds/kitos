using CSharpFunctionalExtensions;
using PubSub.Core.Models;

namespace PubSub.DataAccess;

public interface ISubscriptionRepository
{
    Task<IEnumerable<Subscription>> GetAllAsync();
    Task<Maybe<Subscription>> GetAsync(Guid uuid);
    Task AddAsync(Subscription subscription);
    Task UpdateAsync(Subscription subscription);
    Task DeleteAsync(Guid uuid);
}