using System;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

    public TokenFetcher(string clientCertificateThumbprint, string stsIssuer, string stsEndpoint, string stsCertificateAlias, string stsCertificateThumbprint, string wspCertificateThumbprint)
    {
        _clientCertificateThumbprint = clientCertificateThumbprint;
        _stsIssuer = stsIssuer;
        _stsEndpoint = stsEndpoint;
        _stsCertificateAlias = stsCertificateAlias;
        _stsCertificateThumbprint = stsCertificateThumbprint;
        _wspCertificateThumbprint = wspCertificateThumbprint;
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
        var clientCert = CertificateLoader.LoadCertificate(StoreName.My, StoreLocation.LocalMachine, _clientCertificateThumbprint);
        var stsCert = CertificateLoader.LoadCertificate(StoreName.My, StoreLocation.LocalMachine, _stsCertificateThumbprint);
        var wspCert = CertificateLoader.LoadCertificate(StoreName.My, StoreLocation.LocalMachine, _wspCertificateThumbprint);
        var stsConfig = new StsConfiguration(_stsEndpoint, _stsIssuer, cvr, stsCert);
        var wspConfig = new WspConfiguration(entityId, wspEndpoint, System.ServiceModel.EnvelopeVersion.Soap12, wspCert);
        return new StsTokenServiceConfiguration(stsConfig, wspConfig, clientCert);
    }

    private SecurityToken FetchToken(string entityId, string cvr)
    {
        var config = BuildServiceConfig(entityId, entityId, cvr);
        return new StsTokenService(config).GetToken();
    }
}
