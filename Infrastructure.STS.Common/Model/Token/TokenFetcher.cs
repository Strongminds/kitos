﻿using System;
using System.Diagnostics;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using Infrastructure.STS.Common.Model.Token;

namespace Kombit.InfrastructureSamples.Token;

public static class TokenFetcher
{
    /// <summary>
    ///     Checks if a token is valid. 5 minutes added to be sure it is valid (can be changed)
    /// </summary>
    /// <param name="token">The token to check</param>
    /// <returns>True of token is expired, false otherwise</returns>
    public static bool IsTokenExpired(SecurityToken token)
    {
        Debug.WriteLine("TokenId:" + token.Id);
        return token.ValidTo > DateTime.Now.AddMinutes(5);
    }

    /// <summary>
    ///     Creates a token for use on Serviceplatformen
    /// </summary>
    /// <param name="entityId">The namespace to get a token for</param>
    /// <returns>A new token</returns>
    public static SecurityToken IssueToken(string entityId, string cvr)
    {
        SecurityToken token = null;

        var certificate = CertificateLoader.LoadCertificate(
            StoreName.My,
            StoreLocation.LocalMachine,
            ConfigVariables.ClientCertificateThumbprint);

        var absoluteUri = new Uri(entityId).AbsoluteUri;
        var cacheKey =
            new Guid(MD5.Create()
                .ComputeHash(Encoding.Default.GetBytes(absoluteUri + "_" + cvr))).ToString();
        var inCache = CacheHelper.IsIncache(cacheKey);
        var needNewToken = false;

        if (inCache)
        {
            token = CacheHelper.GetFromCache<GenericXmlSecurityToken>(cacheKey);

            // TODO: Currently this doesn't handle the timezone difference, so there is a one hour gap. 
            if (token.ValidTo.CompareTo(DateTime.Now) < 0)
                needNewToken = true;
        }

        if (inCache == false || needNewToken)
        {
            token = SendSecurityTokenRequest(absoluteUri, certificate, cvr);
            CacheHelper.SaveTocache(cacheKey, token, token.ValidTo);
        }

        return token;
    }

    private static SecurityToken SendSecurityTokenRequest(string appliesTo, X509Certificate2 clientCertificate,
        string cvr)
    {
        var rst = new RequestSecurityToken
        {
            AppliesTo = new EndpointReference(appliesTo),
            RequestType = RequestTypes.Issue,
            TokenType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0",
            KeyType = KeyTypes.Asymmetric,
            Issuer = new EndpointReference(ConfigVariables.StsIssuer),
            UseKey = new UseKey(new X509SecurityToken(clientCertificate))
        };

        rst.Claims.Dialect = "http://docs.oasis-open.org/wsfed/authorization/200706/authclaims";
        rst.Claims.Add(new RequestClaim("dk:gov:saml:attribute:CvrNumberIdentifier", false, cvr));


        var client = GenerateStsCertificateClientChannel(clientCertificate);
        return client.Issue(rst);
    }

    private static IWSTrustChannelContract GenerateStsCertificateClientChannel(X509Certificate2 clientCertificate)
    {
        var stsAddress = new EndpointAddress(new Uri(ConfigVariables.StsEndpoint),
            EndpointIdentity.CreateDnsIdentity(ConfigVariables.StsCertificateAlias));
        var binding = new MutualCertificateWithMessageSecurityBinding(null);
        var factory = new WSTrustChannelFactory(binding, stsAddress);

        factory.TrustVersion = TrustVersion.WSTrust13;
        factory.Credentials.ClientCertificate.Certificate = clientCertificate;
        var certificate = CertificateLoader.LoadCertificate(
            StoreName.My,
            StoreLocation.LocalMachine,
            ConfigVariables.StsCertificateThumbprint);
        factory.Credentials.ServiceCertificate.ScopedCertificates.Add(stsAddress.Uri, certificate);
        factory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode =
            X509CertificateValidationMode.None;
        // Disable revocation checking (do not use in production)
        // Should be uncommented if you intent to call DemoService locally.
        // factory.Credentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
        factory.Endpoint.Contract.ProtectionLevel = ProtectionLevel.Sign;


        return factory.CreateChannel();
    }
}