using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Presentation.Web.Infrastructure.Attributes.RateLimiters;

public class FixedWindowLimiter : IRateLimiter
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly Timer _cleanupTimer;
    private readonly ConcurrentDictionary<string, (DateTime start, int count)> _store
        = new();

    public FixedWindowLimiter(int maxRequests, TimeSpan window)
    {
        _maxRequests = maxRequests;
        _window = window;
        _cleanupTimer = new Timer(_ => Cleanup(), null, window, window);
    }

    public bool ShouldLimit(string key)
    {
        if (!_store.TryGetValue(key, out var entry)) return false;
        var (windowStart, count) = entry;
        var now = DateTime.UtcNow;

        if (now - windowStart > _window)
        {
            return false;
        }
        return count >= _maxRequests;
    }

    public void RecordFailure(string key)
    {
        var now = DateTime.UtcNow;

        _store.AddOrUpdate(
            key,
            _ => (now, 1),
            (_, old) =>
            {
                var (windowStart, oldCount) = old;
                if (now - windowStart > _window)
                {
                    return (now, 1);
                }
                else
                {
                    return (windowStart, oldCount + 1);
                }
            });
    }

    private void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _store)
        {
            if (now - kvp.Value.start > _window)
            {
                _store.TryRemove(kvp.Key, out _);
            }
        }
    }

}