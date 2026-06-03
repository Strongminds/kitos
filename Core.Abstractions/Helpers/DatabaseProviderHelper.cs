using System;

namespace Core.Abstractions.Helpers
{
    public static class DatabaseProviderHelper
    {
        public static bool IsPostgreSqlProvider(string? provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return false;
            }

            return string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase);
        }
    }
}
