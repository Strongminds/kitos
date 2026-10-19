using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    public static string[] GetAllowedThumbprints(IConfiguration configuration) =>
        (configuration["AppSettings:BeskedfordelerCertificateThumbprint"] ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool IsValid(X509Certificate2? certificate)
    {
        if (certificate == null)
        {
            logger.LogWarning("Beskedfordeler certificate validation failed: no client certificate.");
            return false;
        }
        var allowed = GetAllowedThumbprints(configuration);
        var thumbprint = certificate.Thumbprint;
        if (!allowed.Contains(thumbprint, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Beskedfordeler certificate thumbprint is not allowlisted. CertificateThumbprint={CertificateThumbprint}, AllowedThumbprintCount={AllowedThumbprintCount}.",
                thumbprint, allowed.Length);
            return false;
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
        chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);
        logger.LogDebug(
            "Beskedfordeler certificate thumbprint is allowlisted; building client-authentication chain with online revocation excluding root and a 10-second URL retrieval timeout. CertificateThumbprint={CertificateThumbprint}.",
            thumbprint);
        var timer = Stopwatch.StartNew();
        var valid = chain.Build(certificate);
        logger.LogDebug(
            "Beskedfordeler certificate chain validation completed. CertificateThumbprint={CertificateThumbprint}, IsValid={IsValid}, ElapsedMilliseconds={ElapsedMilliseconds}, ChainElementCount={ChainElementCount}.",
            thumbprint, valid, timer.ElapsedMilliseconds, chain.ChainElements.Count);
        if (!valid)
        {
            logger.LogWarning(
                "Beskedfordeler certificate chain rejected. CertificateThumbprint={CertificateThumbprint}, ChainStatus={ChainStatus}.",
                thumbprint, string.Join(", ", chain.ChainStatus.Select(status => status.Status)));
            for (var index = 0; index < chain.ChainElements.Count; index++)
            {
                var element = chain.ChainElements[index];
                logger.LogDebug(
                    "Beskedfordeler certificate chain element. CertificateThumbprint={CertificateThumbprint}, ElementIndex={ElementIndex}, ElementThumbprint={ElementThumbprint}, ChainStatus={ChainStatus}.",
                    thumbprint, index, element.Certificate.Thumbprint,
                    string.Join(", ", element.ChainElementStatus.Select(status => status.Status)));
            }
        }
        return valid;
    }
}
