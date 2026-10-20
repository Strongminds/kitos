using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Infrastructure.STS.Beskedfordeler;

public interface IBeskedfordelerClientCertificateValidator
{
    bool IsValid(X509Certificate2? certificate);
}

public sealed class BeskedfordelerClientCertificateValidator(
    IConfiguration configuration, ILogger<BeskedfordelerClientCertificateValidator> logger) : IBeskedfordelerClientCertificateValidator
{
    public static string[] GetAllowedThumbprints(IConfiguration configuration) =>
        (configuration["AppSettings:BeskedfordelerCertificateThumbprint"] ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool IsValid(X509Certificate2? certificate)
    {
        if (certificate == null)
        {
            logger.LogWarning("Beskedfordeler certificate validation rejected: no client certificate.");
            return false;
        }
        var allowed = GetAllowedThumbprints(configuration);
        var thumbprint = certificate.Thumbprint;
        logger.LogWarning("Beskedfordeler certificate validation invoked. Thumbprint {Thumbprint}, AllowedCertificateCount {AllowedCertificateCount}.",
            thumbprint, allowed.Length);
        if (!allowed.Contains(thumbprint, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning("Beskedfordeler certificate rejected: thumbprint not allowlisted. Thumbprint {Thumbprint}.", thumbprint);
            return false;
        }

        logger.LogWarning("Beskedfordeler certificate thumbprint matched; validating chain with online revocation and client authentication usage. Thumbprint {Thumbprint}.", thumbprint);
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
        chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);
        try
        {
            var valid = chain.Build(certificate);
            logger.LogWarning("Beskedfordeler certificate chain validation completed. Thumbprint {Thumbprint}, Valid {Valid}, ChainStatus {ChainStatus}.",
                thumbprint, valid, string.Join(", ", chain.ChainStatus.Select(status => status.Status.ToString())));
            return valid;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Beskedfordeler certificate chain validation failed. Thumbprint {Thumbprint}, ExceptionType {ExceptionType}.",
                thumbprint, ex.GetType().Name);
            throw;
        }
    }
}
