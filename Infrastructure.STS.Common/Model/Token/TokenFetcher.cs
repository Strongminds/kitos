using System;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Text;
using Digst.OioIdws.CommonCore;
using Digst.OioIdws.OioWsTrustCore;
using Infrastructure.STS.Common.Model.Token;

namespace Kombit.InfrastructureSamples.Token;

public class TokenFetcher
{
    private readonly string _clientCertificateThumbprint;
    private readonly string _stsIssuer;
    private readonly string _stsEndpoint;
    private readonly string _stsCertificateAlias;
    private readonly string _stsCertificateThumbprint;
    private readonly string _wspCertificateThumbprint;

    private readonly string? _ssoCertFilePath;
    private readonly string? _ssoCertPassword;
    private readonly string? _stsCertFilePath;
    private readonly string? _stsCertPassword;
    private readonly string? _stsOrganisationCertFilePath;
    private readonly string? _stsOrganisationCertPassword;
    private readonly X509CertificateValidationMode? _serviceCertificateValidationMode;
    private readonly X509RevocationMode? _serviceCertificateRevocationMode;

    public TokenFetcher(
        string clientCertificateThumbprint,
        string stsIssuer,
        string stsEndpoint,
        string stsCertificateAlias,
        string stsCertificateThumbprint,
        string wspCertificateThumbprint,
        string? ssoCertFilePath = null,
        string? ssoCertPassword = null,
        string? stsCertFilePath = null,
        string? stsCertPassword = null,
        string? stsOrganisationCertFilePath = null,
        string? stsOrganisationCertPassword = null,
        string? serviceCertificateValidationMode = null,
        string? serviceCertificateRevocationMode = null)
    {
        _clientCertificateThumbprint = clientCertificateThumbprint;
        _stsIssuer = stsIssuer;
        _stsEndpoint = stsEndpoint;
        _stsCertificateAlias = stsCertificateAlias;
        _stsCertificateThumbprint = stsCertificateThumbprint;
        _wspCertificateThumbprint = wspCertificateThumbprint;
        _ssoCertFilePath = ssoCertFilePath;
        _ssoCertPassword = ssoCertPassword;
        _stsCertFilePath = stsCertFilePath;
        _stsCertPassword = stsCertPassword;
        _stsOrganisationCertFilePath = stsOrganisationCertFilePath;
        _stsOrganisationCertPassword = stsOrganisationCertPassword;
        _serviceCertificateValidationMode = ParseOptionalEnum<X509CertificateValidationMode>(
            serviceCertificateValidationMode,
            nameof(serviceCertificateValidationMode));
        _serviceCertificateRevocationMode = ParseOptionalEnum<X509RevocationMode>(
            serviceCertificateRevocationMode,
            nameof(serviceCertificateRevocationMode));
    }

    /// <summary>
    /// Checks if a token is valid. 5 minutes added to be sure it is valid.
    /// </summary>
    public bool IsTokenExpired(SecurityToken token)
    {
        return token.ValidTo > DateTime.Now.AddMinutes(5);
    }

    /// <summary>
    /// Creates a token for use on Serviceplatformen using Kombit.OioIdws.WscCore.
    /// </summary>
    public SecurityToken IssueToken(string entityId, string cvr)
    {
        var absoluteUri = new Uri(entityId).AbsoluteUri;
        var cacheKey = new Guid(MD5.HashData(Encoding.Default.GetBytes(absoluteUri + "_" + cvr))).ToString();
        var inCache = CacheHelper.IsIncache(cacheKey);

        if (inCache)
        {
            var cached = CacheHelper.GetFromCache<GenericXmlSecurityToken>(cacheKey);
            if (cached != null && cached.ValidTo.CompareTo(DateTime.Now) >= 0)
                return cached;
        }

        var token = FetchToken(absoluteUri, cvr);
        CacheHelper.SaveTocache(cacheKey, token, token.ValidTo);
        return token;
    }

    /// <summary>
    /// Builds a StsTokenServiceConfiguration with the correct WSP service endpoint URL.
    /// Use for creating WCF channels via FederatedChannelFactoryExtensions.CreateChannelWithIssuedToken.
    /// </summary>
    public StsTokenServiceConfiguration BuildServiceConfig(string entityId, string wspEndpoint, string cvr)
    {
        var clientCert = CertificateLoader.LoadCertificateWithFallback(
            _ssoCertFilePath, _ssoCertPassword,
            StoreName.My, StoreLocation.LocalMachine, _clientCertificateThumbprint);
        var stsCert = CertificateLoader.LoadCertificateWithFallback(
            _stsCertFilePath, _stsCertPassword,
            StoreName.My, StoreLocation.LocalMachine, _stsCertificateThumbprint);
        var wspCert = CertificateLoader.LoadCertificateWithFallback(
            _stsOrganisationCertFilePath, _stsOrganisationCertPassword,
            StoreName.My, StoreLocation.LocalMachine, _wspCertificateThumbprint);
        var stsConfig = new StsConfiguration(_stsEndpoint, _stsIssuer, cvr, stsCert);
        var wspConfig = new WspConfiguration(wspEndpoint, entityId, System.ServiceModel.EnvelopeVersion.Soap12, wspCert);
        var config = new StsTokenServiceConfiguration(stsConfig, wspConfig, clientCert)
        {
            MaxReceivedMessageSize = int.MaxValue
        };
        ConfigureServiceCertificateAuthentication(config);
        return config;
    }

    /// <summary>
    /// Issues a STS token and creates a WCF channel to <paramref name="wspEndpoint"/>.
    /// Uses <see cref="FixedCreateChannelExtensions"/> instead of the Kombit
    /// <see cref="Digst.OioIdws.SoapCore.FederatedChannelFactoryExtensions"/> to work around
    /// a .NET 10 WCF incompatibility in <c>FederatedChannelFactory.ApplyConfiguration</c>.
    /// </summary>
    public T CreateChannel<T>(string entityId, string wspEndpoint, string cvr)
    {
        var token = (GenericXmlSecurityToken)IssueToken(entityId, cvr);
        var config = BuildServiceConfig(entityId, wspEndpoint, cvr);
        return FixedCreateChannelExtensions.CreateChannelWithIssuedToken<T>(token, config);
    }

    private SecurityToken FetchToken(string entityId, string cvr)
    {
        var config = BuildServiceConfig(entityId, entityId, cvr);
        return new StsTokenService(config).GetToken();
    }

    private void ConfigureServiceCertificateAuthentication(IStsTokenServiceConfiguration config)
    {
        ApplyAuthenticationOverrides(config.StsCertificateAuthentication);
        ApplyAuthenticationOverrides(config.WspCertificateAuthentication);
        ApplyAuthenticationOverrides(config.SslCertificateAuthentication);
    }

    private void ApplyAuthenticationOverrides(X509ServiceCertificateAuthentication authentication)
    {
        if (_serviceCertificateValidationMode.HasValue)
            authentication.CertificateValidationMode = _serviceCertificateValidationMode.Value;
        if (_serviceCertificateRevocationMode.HasValue)
            authentication.RevocationMode = _serviceCertificateRevocationMode.Value;
    }

    private static TEnum? ParseOptionalEnum<TEnum>(string? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
            return parsed;
        throw new ArgumentException(
            $"Invalid value '{value}' for {parameterName}. Expected one of: {string.Join(", ", Enum.GetNames(typeof(TEnum)))}",
            parameterName);
    }
}
