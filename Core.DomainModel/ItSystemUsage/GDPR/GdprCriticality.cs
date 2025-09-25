using System;

namespace Core.DomainModel.ItSystemUsage.GDPR
{
    public enum GdprCriticality
    {
        NotCritical = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        VeryHigh = 4
    }

    public static class GdprCriticalityExtensions
    {
        public static string GetReadableName(this GdprCriticality gdprCriticality)
        {
            return gdprCriticality switch
            {
                GdprCriticality.NotCritical => "Ikke kritisk",
                GdprCriticality.Low => "Lav",
                GdprCriticality.Medium => "Medium",
                GdprCriticality.High => "Høj",
                GdprCriticality.VeryHigh => "Meget høj",
                _ => throw new InvalidOperationException($"Invalid gdprCriticality value: {gdprCriticality}")
            };
        }
    }
}
