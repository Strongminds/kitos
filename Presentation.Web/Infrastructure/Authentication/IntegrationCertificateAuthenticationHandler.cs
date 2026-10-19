using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Presentation.Web.Infrastructure.Authentication;

public sealed class IntegrationCertificateAuthenticationOptions : AuthenticationSchemeOptions
{
    public Func<X509Certificate2, bool> ValidateCertificate { get; set; } = _ => false;
}

public sealed class IntegrationCertificateAuthenticationHandler(
    IOptionsMonitor<IntegrationCertificateAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<IntegrationCertificateAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "IntegrationCertificate";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tls = Context.Features.Get<ITlsHandshakeFeature>();
        Logger.LogDebug(
            "Integration certificate authentication started. ConnectionId={ConnectionId}, TraceId={TraceId}, IsHttps={IsHttps}, HttpProtocol={HttpProtocol}, TlsProtocol={TlsProtocol}, CipherSuite={CipherSuite}.",
            Context.Connection.Id, Context.TraceIdentifier, Request.IsHttps, Request.Protocol, tls?.Protocol, tls?.NegotiatedCipherSuite);
        if (!Request.IsHttps)
        {
            Logger.LogWarning("Integration certificate authentication requires HTTPS. ConnectionId={ConnectionId}, TraceId={TraceId}.",
                Context.Connection.Id, Context.TraceIdentifier);
            return AuthenticateResult.NoResult();
        }
        // The TLS handshake proves possession of the private key. Never accept certificate headers.
        Logger.LogDebug("Retrieving integration client certificate from the server connection. ConnectionId={ConnectionId}, TraceId={TraceId}.",
            Context.Connection.Id, Context.TraceIdentifier);
        var certificate = await Context.Connection.GetClientCertificateAsync(Context.RequestAborted);
        if (certificate == null)
        {
            Logger.LogWarning(
                "No integration client certificate was supplied by the server connection. Check TLS client certificate negotiation at Kestrel/IIS. ConnectionId={ConnectionId}, TraceId={TraceId}.",
                Context.Connection.Id, Context.TraceIdentifier);
            return AuthenticateResult.NoResult();
        }

        var thumbprint = certificate.Thumbprint;
        Logger.LogDebug(
            "Integration client certificate received. CertificateThumbprint={CertificateThumbprint}, NotBeforeUtc={NotBeforeUtc}, NotAfterUtc={NotAfterUtc}, ConnectionId={ConnectionId}, TraceId={TraceId}.",
            thumbprint, certificate.NotBefore.ToUniversalTime(), certificate.NotAfter.ToUniversalTime(), Context.Connection.Id, Context.TraceIdentifier);
        if (!Options.ValidateCertificate(certificate))
        {
            Logger.LogWarning(
                "Integration client certificate authentication failed. CertificateThumbprint={CertificateThumbprint}, ConnectionId={ConnectionId}, TraceId={TraceId}.",
                thumbprint, Context.Connection.Id, Context.TraceIdentifier);
            return AuthenticateResult.Fail("Invalid integration certificate.");
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, certificate.Thumbprint)], SchemeName);
        Logger.LogInformation(
            "Integration client certificate authentication succeeded. CertificateThumbprint={CertificateThumbprint}, ConnectionId={ConnectionId}, TraceId={TraceId}.",
            thumbprint, Context.Connection.Id, Context.TraceIdentifier);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Logger.LogWarning("Integration certificate authentication challenge returning HTTP 403. ConnectionId={ConnectionId}, TraceId={TraceId}.",
            Context.Connection.Id, Context.TraceIdentifier);
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
