using System;

namespace Core.DomainModel.ItSystemUsage.GDPR
{
    public enum GdprCriticality
    {
        NOT_CRITICAL = 0,
        LOW = 1,
        MEDIUM = 2,
        HIGH = 3,
        VERY_HIGH = 4
    }

    public static class GdprCriticalityExtensions
    {
        public static string GetReadableName(this GdprCriticality gdprCriticality)
        {
            return gdprCriticality switch
            {
                GdprCriticality.NOT_CRITICAL => "Ikke kritisk",
                GdprCriticality.LOW => "Lav",
                GdprCriticality.MEDIUM => "Medium",
                GdprCriticality.HIGH => "Høj",
                GdprCriticality.VERY_HIGH => "Meget høj",
                _ => throw new InvalidOperationException($"Invalid gdprCriticality value: {gdprCriticality}")
            };
        }
    }
}
