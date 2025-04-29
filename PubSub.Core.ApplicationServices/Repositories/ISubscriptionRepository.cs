using CSharpFunctionalExtensions;
using PubSub.Core.DomainModel;

namespace PubSub.Core.ApplicationServices.Repositories;

public interface ISubscriptionRepository
{
    Task<IEnumerable<Subscription>> GetAllByUserId(string userId);
    Task<IEnumerable<Subscription>> GetByTopic(string topic);
    Task<bool> Exists(string topic, string url);
    Task<Maybe<Subscription>> GetAsync(Guid uuid);
    Task AddAsync(Subscription subscription);
    Task DeleteAsync(Subscription subscription);
}