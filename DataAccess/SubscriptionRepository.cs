using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using PubSub.Core.Models;
using PubSub.DataAccess.Helpers;

namespace PubSub.DataAccess;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly PubSubContext _context;
    public SubscriptionRepository(PubSubContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Subscription>> GetAllAsync()
    {
        return await _context.Subscriptions.ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetByTopic(string topic)
    {
        return await _context.Subscriptions.Where(x => x.Topic.Name == topic).ToListAsync();
    }

    public async Task<Maybe<Subscription>> GetAsync(Guid uuid)
    {
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(x => x.Uuid == uuid);
        return Maybe<Subscription>.From(subscription);
    }


    public async Task AddAsync(Subscription subscription)
    {
        await _context.Subscriptions.AddAsync(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Subscription> subscriptions)
    {
        await _context.Subscriptions.AddRangeAsync(subscriptions);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Subscription subscription)
    {
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid uuid)
    {

        var subscription = await GetAsync(uuid);
        subscription.Tap(DeleteSubscription);
    }

    private async void DeleteSubscription(Subscription subscription)
    {
        _context.Subscriptions.Remove(subscription);
        await _context.SaveChangesAsync();
    }
}