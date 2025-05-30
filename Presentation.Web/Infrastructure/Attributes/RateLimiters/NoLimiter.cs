namespace Presentation.Web.Infrastructure.Attributes.RateLimiters;

public class NoLimiter : IRateLimiter
{
    public bool ShouldLimit(string ip)
    {
        return false;
    }
}