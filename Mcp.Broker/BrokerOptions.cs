using Mcp.ReadBoundary.Core;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Mcp.Broker
{
    public sealed class BrokerOptions
    {
        public required Uri PublicOrigin { get; init; }
        public int ReadPort { get; init; } = 7446;
        public required Uri WorkerOrigin { get; init; }
        public required Uri KitosOrigin { get; init; }
        public required string ClientId { get; init; }
        public required string RedirectUri { get; init; }
        public bool EnableDynamicClientRegistration { get; init; }
        public string? ResourceIdentifier { get; init; }
        public required X509Certificate2 ServerCertificate { get; init; }
        public required X509Certificate2 WorkloadCertificate { get; init; }
        public required X509Certificate2 SigningCertificate { get; init; }
        public required X509Certificate2 EncryptionCertificate { get; init; }
        public required string WorkerClientCertificateSha256 { get; init; }
        public required string WorkerServerCertificateSha256 { get; init; }
        public string Resource => ResourceIdentifier ?? new Uri(PublicOrigin, "/mcp").AbsoluteUri;

        public void Validate()
        {
            _ = new ReadRequestPolicy(PublicOrigin);
            _ = new ReadRequestPolicy(WorkerOrigin);
            _ = new ReadRequestPolicy(KitosOrigin);
            if (ResourceIdentifier is not null && (!Uri.TryCreate(ResourceIdentifier, UriKind.Absolute, out var resource) ||
                resource.Scheme != Uri.UriSchemeHttps || !resource.IsWellFormedOriginalString() ||
                resource.UserInfo.Length != 0 || resource.Query.Length != 0 || resource.Fragment.Length != 0))
                throw new InvalidOperationException("Broker OAuth resource identifier must be an absolute HTTPS identifier without credentials, query or fragment.");
            if (ReadPort is < 1 or > 65535 || ReadPort == PublicOrigin.Port ||
                string.IsNullOrWhiteSpace(ClientId) || !Uri.TryCreate(RedirectUri, UriKind.Absolute, out var redirect) ||
                redirect.Scheme != Uri.UriSchemeHttps || redirect.UserInfo.Length != 0 || redirect.Fragment.Length != 0 ||
                new[] { WorkerClientCertificateSha256, WorkerServerCertificateSha256 }.Any(pin => pin.Length != 64 || !pin.All(Uri.IsHexDigit)) ||
                new[] { ServerCertificate, WorkloadCertificate, SigningCertificate, EncryptionCertificate }.Any(cert =>
                    !cert.HasPrivateKey || cert.NotAfter.ToUniversalTime() <= DateTime.UtcNow || cert.NotBefore.ToUniversalTime() > DateTime.UtcNow))
                throw new InvalidOperationException("Broker requires exact HTTPS client registration, valid owned certificates and a SHA256 worker certificate pin.");
            if (EnableDynamicClientRegistration && !System.Text.RegularExpressions.Regex.IsMatch(RedirectUri,
                @"\Ahttps://chatgpt\.com(?::443)?/connector/oauth/[A-Za-z0-9]+\z", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
                throw new InvalidOperationException("Pilot dynamic registration requires the exact configured ChatGPT HTTPS connector callback.");
        }

        public bool IsWorker(X509Certificate2? certificate) => Matches(certificate, WorkerClientCertificateSha256);
        private static bool Matches(X509Certificate2? certificate, string pin) => certificate is not null &&
            certificate.NotBefore.ToUniversalTime() <= DateTime.UtcNow && certificate.NotAfter.ToUniversalTime() > DateTime.UtcNow &&
            CryptographicOperations.FixedTimeEquals(certificate.GetCertHash(HashAlgorithmName.SHA256), Convert.FromHexString(pin));

        internal HttpClient CreateWorkerClient()
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
            handler.ClientCertificates.Add(WorkloadCertificate);
            // The configured exact certificate pin is the pilot's private trust anchor; hostname validation remains mandatory.
            handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
                (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None && Matches(certificate, WorkerServerCertificateSha256);
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        }
    }
}
