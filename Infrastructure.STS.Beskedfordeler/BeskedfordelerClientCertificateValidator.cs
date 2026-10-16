using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.STS.Beskedfordeler;

public interface IBeskedfordelerClientCertificateValidator
{
    bool IsValid(X509Certificate2? certificate);
}

public sealed class BeskedfordelerClientCertificateValidator(
    IConfiguration configuration,
    ILogger<BeskedfordelerClientCertificateValidator> logger) : IBeskedfordelerClientCertificateValidator
{
    public static string[] GetAllowedFingerprints(IConfiguration configuration) =>
        (configuration["AppSettings:BeskedfordelerCertificateSha256"] ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool IsValid(X509Certificate2? certificate)
    {
        if (certificate == null)
        {
            logger.LogWarning("Beskedfordeler certificate validation failed: no client certificate.");
            return false;
        }
        var allowed = GetAllowedFingerprints(configuration);
        var fingerprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);
        if (!allowed.Contains(fingerprint, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Beskedfordeler certificate fingerprint is not allowlisted. CertificateSha256={CertificateSha256}, AllowedFingerprintCount={AllowedFingerprintCount}.",
                fingerprint, allowed.Length);
            return false;
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
        chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);
        logger.LogDebug(
            "Beskedfordeler certificate fingerprint is allowlisted; building client-authentication chain with online revocation excluding root and a 10-second URL retrieval timeout. CertificateSha256={CertificateSha256}.",
            fingerprint);
        var timer = Stopwatch.StartNew();
        var valid = chain.Build(certificate);
        logger.LogDebug(
            "Beskedfordeler certificate chain validation completed. CertificateSha256={CertificateSha256}, IsValid={IsValid}, ElapsedMilliseconds={ElapsedMilliseconds}, ChainElementCount={ChainElementCount}.",
            fingerprint, valid, timer.ElapsedMilliseconds, chain.ChainElements.Count);
        if (!valid)
        {
            logger.LogWarning(
                "Beskedfordeler certificate chain rejected. CertificateSha256={CertificateSha256}, ChainStatus={ChainStatus}.",
                fingerprint, string.Join(", ", chain.ChainStatus.Select(status => status.Status)));
            for (var index = 0; index < chain.ChainElements.Count; index++)
            {
                var element = chain.ChainElements[index];
                logger.LogDebug(
                    "Beskedfordeler certificate chain element. CertificateSha256={CertificateSha256}, ElementIndex={ElementIndex}, ElementSha256={ElementSha256}, ChainStatus={ChainStatus}.",
                    fingerprint, index, element.Certificate.GetCertHashString(HashAlgorithmName.SHA256),
                    string.Join(", ", element.ChainElementStatus.Select(status => status.Status)));
            }
        }
        return valid;
    }
}
