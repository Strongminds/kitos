using System;

namespace Presentation.Web.Infrastructure.Attributes.RateLimiters;

public interface IRateLimiter
{
    bool ShouldLimit(string key);

    void RecordFailure(string key);
}