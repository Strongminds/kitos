﻿using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using PubSub.Core.Abstractions.ErrorTypes;
using PubSub.Core.Models;

namespace PubSub.DataAccess;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly PubSubContext _context;
    public SubscriptionRepository(PubSubContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Subscription>> GetAllByUserId(string userId)
    {
        return await _context.Subscriptions.Where(x => x.OwnerId == userId).ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetByTopic(string topic)
    {
        return await SubscriptionsByTopic(topic).ToListAsync();
    }

    public async Task<bool> Exists(string topic, string url)
    {
        return await SubscriptionsByTopic(topic).Where(x => x.Callback == url).AnyAsync();
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

    public async Task<Maybe<OperationError>> DeleteAsync(Guid uuid)
    {
        var maybeSubscription = await GetAsync(uuid);
        if (maybeSubscription.HasNoValue)
        {
            return OperationError.NotFound; //TODO
        }
        await DeleteSubscription(maybeSubscription.Value);
        return Maybe<OperationError>.None;
    }

    private async Task DeleteSubscription(Subscription subscription)
    {
        _context.Subscriptions.Remove(subscription);
        await _context.SaveChangesAsync();
    }

    private IQueryable<Subscription> SubscriptionsByTopic(string topic)
    {
        return _context.Subscriptions.Where(x => x.Topic == topic);
    }
}