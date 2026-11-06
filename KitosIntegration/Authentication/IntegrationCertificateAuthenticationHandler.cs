using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KitosIntegration.Authentication;

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
        Logger.LogWarning("Beskedfordeler certificate authentication invoked. TraceId {TraceId}, IsHttps {IsHttps}.",
            Context.TraceIdentifier, Request.IsHttps);
        if (!Request.IsHttps)
        {
            Logger.LogWarning("Beskedfordeler certificate authentication stopped: HTTPS required. TraceId {TraceId}.", Context.TraceIdentifier);
            return AuthenticateResult.NoResult();
        }
        try
        {
            // The TLS handshake proves possession of the private key. Never accept certificate headers.
            var certificate = await Context.Connection.GetClientCertificateAsync(Context.RequestAborted);
            if (certificate == null)
            {
                Logger.LogWarning("Beskedfordeler certificate authentication stopped: no TLS client certificate received. TraceId {TraceId}.", Context.TraceIdentifier);
                return AuthenticateResult.NoResult();
            }

            Logger.LogWarning("Beskedfordeler TLS client certificate received; invoking validation. TraceId {TraceId}, Thumbprint {Thumbprint}.",
                Context.TraceIdentifier, certificate.Thumbprint);
            if (!Options.ValidateCertificate(certificate))
            {
                Logger.LogWarning("Beskedfordeler certificate authentication rejected. TraceId {TraceId}, Thumbprint {Thumbprint}.",
                    Context.TraceIdentifier, certificate.Thumbprint);
                return AuthenticateResult.Fail("Invalid integration certificate.");
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, certificate.Thumbprint)], SchemeName);
            Logger.LogWarning("Beskedfordeler certificate authentication succeeded. TraceId {TraceId}, Thumbprint {Thumbprint}.",
                Context.TraceIdentifier, certificate.Thumbprint);
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Beskedfordeler certificate authentication cancelled. TraceId {TraceId}.", Context.TraceIdentifier);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Beskedfordeler certificate authentication failed. TraceId {TraceId}, ExceptionType {ExceptionType}.",
                Context.TraceIdentifier, ex.GetType().Name);
            throw;
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Logger.LogWarning("Beskedfordeler certificate authentication challenge returning HTTP 403. TraceId {TraceId}.", Context.TraceIdentifier);
        return Task.CompletedTask;
    }
}

