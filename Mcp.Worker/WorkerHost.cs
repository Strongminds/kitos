using Microsoft.AspNetCore.Server.Kestrel.Https;
using ModelContextProtocol.AspNetCore;
using System.Security.Cryptography.X509Certificates;

namespace Mcp.Worker
{
    public static class WorkerHost
    {
        public static WebApplication Create(string[] args, Action<WebApplicationBuilder>? configure = null)
        {
            var builder = WebApplication.CreateBuilder(args);
            // The pilot does not log tool payloads or caller grants through transport logging.
            builder.Logging.ClearProviders();
            configure?.Invoke(builder);
            var options = builder.Configuration.GetSection("Worker").Get<WorkerOptions>()
                ?? throw new InvalidOperationException("Worker configuration is required.");
            options.Validate();
            builder.WebHost.ConfigureKestrel(kestrel =>
            {
                kestrel.Limits.MaxRequestBodySize = 32768;
                kestrel.ConfigureHttpsDefaults(https =>
                {
                    https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    https.ClientCertificateValidation = (certificate, _, _) => PeerCertificate.Matches(certificate, options.BrokerClientCertificateSha256);
                });
            });
            builder.Services.AddSingleton(options);
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<KitosQueryService>();
            builder.Services.AddHttpClient<BoundaryClient>(client =>
            {
                client.BaseAddress = options.BrokerOrigin;
                client.Timeout = TimeSpan.FromSeconds(12);
            }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
                var certificate = X509CertificateLoader.LoadPkcs12FromFile(options.ClientCertificatePath, null);
                if (!certificate.HasPrivateKey || certificate.NotBefore.ToUniversalTime() > DateTime.UtcNow || certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow)
                    throw new InvalidOperationException("Worker client certificate needs a valid private key.");
                handler.ClientCertificates.Add(certificate);
                handler.ServerCertificateCustomValidationCallback = (_, serverCertificate, _, errors) =>
                    PeerCertificate.MatchesServer(serverCertificate, options.BrokerServerCertificateSha256, errors);
                return handler;
            });
            builder.Services.AddMcpServer()
                .WithHttpTransport(transport => transport.SessionMode = HttpServerSessionMode.Stateless)
                .WithTools<KitosTools>();
            var app = builder.Build();
            app.Use(async (context, next) =>
            {
                var certificate = await context.Connection.GetClientCertificateAsync(context.RequestAborted);
                if (!context.Request.IsHttps || !PeerCertificate.Matches(certificate, options.BrokerClientCertificateSha256) ||
                    !context.Request.Headers.TryGetValue("X-Mcp-Read-Grant", out var grant) || grant.Count != 1 ||
                    !BoundaryClient.IsGrant(grant[0]))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                // The worker has no public/browser endpoint. Only its pinned trusted ingress may call it.
                if (context.Request.Headers.ContainsKey("Origin") || context.Request.Headers.ContainsKey("Authorization"))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                await next(context);
            });
            app.MapMcp("/mcp");
            return app;
        }
    }
}
