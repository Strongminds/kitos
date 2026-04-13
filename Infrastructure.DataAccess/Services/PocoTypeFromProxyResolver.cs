using System;
using Infrastructure.Services.DataAccess;

namespace Infrastructure.DataAccess.Services
{
    /// <summary>
    /// EF Core does not use POCO proxies by default; returns the type directly.
    /// </summary>
    public class PocoTypeFromProxyResolver : IEntityTypeResolver
    {
        public Type Resolve(Type entityType)
        {
            return entityType;
        }
    }
}
