using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Mcp.Worker
{
    public sealed class WorkerOptions
    {
        public required Uri BrokerOrigin { get; init; }
        public required string BrokerClientCertificateSha256 { get; init; }
        public required string BrokerServerCertificateSha256 { get; init; }
        public required string ClientCertificatePath { get; init; }

        public void Validate()
        {
            if (!BrokerOrigin.IsAbsoluteUri || BrokerOrigin.Scheme != "https" || BrokerOrigin.AbsolutePath != "/" ||
                BrokerOrigin.UserInfo.Length != 0 || BrokerOrigin.Query.Length != 0 || BrokerOrigin.Fragment.Length != 0)
                throw new ArgumentException("Worker requires a fixed HTTPS broker origin.");
            if (!PeerCertificate.IsHash(BrokerClientCertificateSha256) || !PeerCertificate.IsHash(BrokerServerCertificateSha256))
                throw new ArgumentException("Worker requires SHA-256 broker certificate pins.");
            if (string.IsNullOrWhiteSpace(ClientCertificatePath)) throw new ArgumentException("Worker requires its client certificate.");
        }
    }

    public static class PeerCertificate
    {
        public static bool IsHash(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

        public static bool Matches(X509Certificate2? certificate, string hash)
        {
            return certificate is not null && IsHash(hash) && certificate.NotBefore.ToUniversalTime() <= DateTime.UtcNow &&
                certificate.NotAfter.ToUniversalTime() > DateTime.UtcNow &&
                CryptographicOperations.FixedTimeEquals(certificate.GetCertHash(HashAlgorithmName.SHA256), Convert.FromHexString(hash));
        }

        public static bool MatchesServer(X509Certificate2? certificate, string hash, SslPolicyErrors errors) =>
            (errors & (SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch)) == 0 &&
            Matches(certificate, hash);
    }
}
