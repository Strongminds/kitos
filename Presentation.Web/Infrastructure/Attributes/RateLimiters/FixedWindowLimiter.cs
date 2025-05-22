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
        var now = DateTime.UtcNow;
        var tuple = _store.AddOrUpdate(key,
            _ => (now, 1),
            (_, old) =>
            {
                if (now - old.start > _window) return (now, 1);
                return (old.start, old.count + 1);
            });
        var shouldBlock = tuple.count > _maxRequests;
        return shouldBlock;
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