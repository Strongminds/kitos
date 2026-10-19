using System.Security.Cryptography;

namespace Mcp.Broker
{
    internal sealed record Credential(string Id, string User, string Jwt, DateTimeOffset Expires, string Version)
    {
        public override string ToString() => "Broker credential [redacted]";
    }
    internal sealed record Grant(string CredentialId, string Version, DateTimeOffset Expires);

    /// <summary>Single-instance ephemeral authority. Every check/admission/replacement is atomic and capacity bounded.</summary>
    internal sealed class PilotState(TimeProvider time)
    {
        private readonly object _sync = new();
        private readonly Dictionary<string, Credential> _credentials = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Grant> _grants = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> _codes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Queue<DateTimeOffset>> _rates = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _active = new(StringComparer.Ordinal);
        private int _total;

        internal Credential? Enroll(string user, string jwt, DateTimeOffset expiry)
        {
            lock (_sync)
            {
                Prune();
                var previous = _credentials.Values.Where(item => item.User == user).ToArray();
                if (expiry <= time.GetUtcNow() || _credentials.Count >= 256 && previous.Length == 0) return null;
                foreach (var old in previous) _credentials.Remove(old.Id);
                var credential = new Credential(Random(), user, jwt, expiry, Random());
                _credentials.Add(credential.Id, credential);
                return credential;
            }
        }

        internal Credential? GetCredential(string? id)
        {
            lock (_sync)
            {
                Prune();
                return id is not null && _credentials.TryGetValue(id, out var value) ? value : null;
            }
        }

        internal void Revoke(string? id)
        {
            lock (_sync)
            {
                if (id is not null) _credentials.Remove(id);
                Prune();
            }
        }
        internal void RevokeUser(string user)
        {
            lock (_sync)
            {
                foreach (var id in _credentials.Values.Where(credential => credential.User == user).Select(credential => credential.Id).ToArray()) _credentials.Remove(id);
                Prune();
            }
        }

        internal string? IssueCode()
        {
            lock (_sync)
            {
                Prune();
                if (_codes.Count >= 512) return null;
                var nonce = Random();
                _codes.Add(Hash(nonce), time.GetUtcNow().AddMinutes(2));
                return nonce;
            }
        }

        internal bool RedeemCode(string? nonce)
        {
            lock (_sync)
            {
                Prune();
                return nonce is not null && _codes.Remove(Hash(nonce));
            }
        }

        internal string? IssueGrant(string id, DateTimeOffset oauthExpiry)
        {
            lock (_sync)
            {
                Prune();
                if (_grants.Count >= 1024 || !_credentials.TryGetValue(id, out var credential)) return null;
                var expires = new[] { oauthExpiry, credential.Expires, time.GetUtcNow().AddMinutes(5) }.Min();
                if (expires <= time.GetUtcNow()) return null;
                var grant = Random();
                _grants.Add(Hash(grant), new Grant(id, credential.Version, expires));
                return grant;
            }
        }

        internal Credential? Resolve(string? grant)
        {
            lock (_sync)
            {
                Prune();
                return grant is not null && grant.Length <= 128 && _grants.TryGetValue(Hash(grant), out var value) &&
                    _credentials.TryGetValue(value.CredentialId, out var credential) && credential.Version == value.Version ? credential : null;
            }
        }

        internal IDisposable? Admit(string user)
        {
            lock (_sync)
            {
                var now = time.GetUtcNow();
                if (!_rates.TryGetValue(user, out var rate))
                {
                    foreach (var stale in _rates.Where(pair => pair.Value.Count == 0 || pair.Value.Last() <= now.AddMinutes(-1)).Select(pair => pair.Key).ToArray()) _rates.Remove(stale);
                    if (_rates.Count >= 256) return null;
                    _rates.Add(user, rate = new Queue<DateTimeOffset>());
                }
                while (rate.TryPeek(out var instant) && instant <= now.AddMinutes(-1)) rate.Dequeue();
                var count = _active.GetValueOrDefault(user);
                if (_total >= 16 || count >= 2 || rate.Count >= 30) return null;
                _total++;
                _active[user] = count + 1;
                rate.Enqueue(now);
                return new Admission(this, user);
            }
        }

        private void Release(string user)
        {
            lock (_sync)
            {
                _total--;
                if (--_active[user] == 0) _active.Remove(user);
            }
        }
        private sealed class Admission(PilotState state, string user) : IDisposable
        {
            private int _disposed;
            public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) state.Release(user); }
        }
        private void Prune()
        {
            var now = time.GetUtcNow();
            foreach (var key in _credentials.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray()) _credentials.Remove(key);
            foreach (var key in _grants.Where(pair => pair.Value.Expires <= now || !_credentials.ContainsKey(pair.Value.CredentialId)).Select(pair => pair.Key).ToArray()) _grants.Remove(key);
            foreach (var key in _codes.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToArray()) _codes.Remove(key);
        }
        private static string Random() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    }
}
