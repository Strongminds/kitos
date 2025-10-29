using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Presentation.Web.Infrastructure.Attributes.RateLimiters;

namespace Presentation.Web.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RateLimitAttribute : ActionFilterAttribute
{
    public IRateLimiter Limiter { get; set; } = new FixedWindowLimiter(10, TimeSpan.FromSeconds(60));

    public override void OnActionExecuting(HttpActionContext actionContext)
    {
        var ip = GetClientIp(actionContext) ?? "unknown";

        var shouldBlock = Limiter.ShouldLimit(ip);

        if (shouldBlock)
        {
            actionContext.Response = actionContext.Request
                .CreateErrorResponse((HttpStatusCode)429, // 429 is the "Too many attempts" status code. Not part of the enum as of writing (22/05/2025)
                    $"Too many attempts. Retry later");
        }
    }

    public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
    {
        var ip = GetClientIp(actionExecutedContext.ActionContext) ?? "unknown";
        var response = actionExecutedContext.Response;
        if (response == null) return;

        // If the action produced any 4xx/5xx, record a failure for this IP:
        if ((int)response.StatusCode == 429) return;
        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 600)
        {
            Limiter.RecordFailure(ip);
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