using Kombit.InfrastructureSamples.Token;

namespace Tools.Beskedfordeler;

public sealed record SenderStsConfiguration(
    string ClientCertificateThumbprint,
    string StsIssuer,
    string StsEndpoint,
    string StsCertificateThumbprint,
    string? SsoCertFilePath = null,
    string? SsoCertPassword = null,
    string? StsCertFilePath = null,
    string? StsCertPassword = null)
{
    public TokenFetcherConfig ToTokenFetcherConfig() =>
        // Token issuance never uses the WSP certificate, but the SOAP library requires one.
        // Reuse the STS certificate only in this AMQP tool; no WSP SOAP channel is created.
        new(ClientCertificateThumbprint, StsIssuer, StsEndpoint, "", StsCertificateThumbprint,
            StsCertificateThumbprint, SsoCertFilePath, SsoCertPassword,
            StsCertFilePath, StsCertPassword, StsCertFilePath, StsCertPassword);
}
