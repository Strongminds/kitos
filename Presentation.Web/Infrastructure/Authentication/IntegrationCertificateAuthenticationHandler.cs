using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
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
        if (!Request.IsHttps) return AuthenticateResult.NoResult();
        // The TLS handshake proves possession of the private key. Never accept certificate headers.
        var certificate = await Context.Connection.GetClientCertificateAsync(Context.RequestAborted);
        if (certificate == null) return AuthenticateResult.NoResult();

        if (!Options.ValidateCertificate(certificate)) return AuthenticateResult.Fail("Invalid integration certificate.");

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, certificate.Thumbprint)], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
