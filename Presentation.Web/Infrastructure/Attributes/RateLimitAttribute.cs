using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Presentation.Web.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RateLimitAttribute : ActionFilterAttribute
{
    // client IP → (windowStartUtc, count)
    private static readonly ConcurrentDictionary<string, (DateTime windowStart, int count)> _requests
        = new ConcurrentDictionary<string, (DateTime, int)>();

    private readonly Timer _cleanupTimer;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    public RateLimitAttribute(int maxRequests = 10, int windowSeconds = 60)
    {
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
        _cleanupTimer = new Timer(_ => Cleanup(), null, windowSeconds, windowSeconds);
    }

    private static void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _requests)
        {
            if (now - kvp.Value.windowStart > TimeSpan.FromMinutes(1))
            {
                _requests.TryRemove(kvp.Key, out _);
            }
        }
    }

    public override void OnActionExecuting(HttpActionContext actionContext)
    {
        var now = DateTime.UtcNow;
        var ip = GetClientIp(actionContext) ?? "unknown";

        _requests.AddOrUpdate(ip,
            addValueFactory: _ => (now, 1),
            updateValueFactory: (_, tuple) =>
            {
                var (start, cnt) = tuple;
                if (now - start > _window)
                    return (now, 1);
                return (start, cnt + 1);
            });

        var entry = _requests[ip];
        if (entry.count > _maxRequests)
        {
            actionContext.Response = actionContext.Request
                .CreateErrorResponse((HttpStatusCode)429,
                    $"Rate limit exceeded. Retry in {(entry.windowStart + _window - now).Seconds}s");
        }
    }

    private string GetClientIp(HttpActionContext context)
    {
        if (context.Request.Properties
                .TryGetValue("MS_HttpContext", out var ctxObj)
            && ctxObj is HttpContextWrapper wrapper)
        {
            return wrapper.Request.UserHostAddress;
        }

        return null;
    }
}