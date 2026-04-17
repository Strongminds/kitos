using System;
using System.Runtime.Caching;
using Core.Abstractions.Caching;
using Core.Abstractions.Types;


namespace Infrastructure.Services.Caching
{
    public class AspNetObjectCache : IObjectCache
    {
        // ignoreConfigSection: true prevents MemoryCache from calling ConfigurationManager,
        // which reads Presentation.Web.dll.config and fails if it contains unrecognized sections (e.g. system.net).
        private static readonly MemoryCache InternalCache = new MemoryCache(nameof(AspNetObjectCache), config: null, ignoreConfigSection: true);

        public void Write<T>(T entry, string key, TimeSpan duration) where T : class
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(duration) };
            InternalCache.Set(key, entry, policy);
        }

        public void Clear(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            InternalCache.Remove(key);
        }

        public Maybe<T> Read<T>(string key) where T : class
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return InternalCache.Get(key) as T;
        }
    }
}
