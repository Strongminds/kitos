using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PubSub.Application.Api.UserSync.Beskedfordeler;

public interface IBeskedfordelerClientCertificateValidator
{
    bool IsValid(X509Certificate2? certificate);
}

public sealed class BeskedfordelerClientCertificateValidator(IConfiguration configuration) : IBeskedfordelerClientCertificateValidator
{
    public bool IsValid(X509Certificate2? certificate)
    {
        if (certificate == null) return false;
        var allowed = configuration.GetSection("Beskedfordeler:ClientCertificateSha256").Get<string[]>() ?? [];
        var fingerprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);
        if (!allowed.Contains(fingerprint, StringComparer.OrdinalIgnoreCase)) return false;

        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
        chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);
        return chain.Build(certificate);
    }
}
