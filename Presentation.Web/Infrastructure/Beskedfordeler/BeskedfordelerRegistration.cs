using Infrastructure.STS.Beskedfordeler;
using Microsoft.AspNetCore.Hosting;

using System;
using System.Linq;
using System.Security.Authentication;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Web.Infrastructure.Authentication;

namespace Presentation.Web.Infrastructure.Beskedfordeler;

public static class BeskedfordelerRegistration
{
    public static void AddBeskedfordeler(this WebApplicationBuilder builder)
    {
        var thumbprints = BeskedfordelerClientCertificateValidator.GetAllowedThumbprints(builder.Configuration);
        if (thumbprints.Length == 0 || thumbprints.Any(value => value.Length != 40 || !value.All(Uri.IsHexDigit)))
            throw new InvalidOperationException("Configure Beskedfordeler client certificate thumbprints (40 hexadecimal characters each).");
        var directory = builder.Configuration["Beskedfordeler:SchemaDirectory"];
        builder.Services.AddSingleton(new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas(directory)));
        builder.Services.AddScoped<BeskedfordelerService>();
        builder.Services.AddSingleton<IBeskedfordelerClientCertificateValidator, BeskedfordelerClientCertificateValidator>();
        builder.Services.AddAuthentication().AddScheme<IntegrationCertificateAuthenticationOptions, IntegrationCertificateAuthenticationHandler>(
            IntegrationCertificateAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddOptions<IntegrationCertificateAuthenticationOptions>(IntegrationCertificateAuthenticationHandler.SchemeName)
            .Configure<IBeskedfordelerClientCertificateValidator>((options, validator) =>
            {
                options.ValidateCertificate = validator.IsValid;
            });
        builder.Services.AddScoped<IBeskedfordelerDeletionMapper, OrgBrugerDeletionMapper>();

        builder.Services.AddScoped<BeskedfordelerJob>();
        builder.Services.AddScoped<global::Infrastructure.STS.Beskedfordeler.BeskedfordelerJob>();
        builder.Services.AddHangfireServer(options =>
        {
            options.Queues = [BeskedfordelerJob.QueueName];
            options.WorkerCount = 2;
        });
        builder.WebHost.ConfigureKestrel(options => options.ConfigureHttpsDefaults(https =>
        {
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        }));
    }

}
