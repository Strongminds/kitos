using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Web.Infrastructure.Beskedfordeler;

public static class BeskedfordelerRegistration
{
    public static void AddBeskedfordeler(this WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue<bool>("Beskedfordeler:Enabled")) return;
        var fingerprints = builder.Configuration.GetSection("Beskedfordeler:ClientCertificateSha256").Get<string[]>() ?? [];
        if (fingerprints.Length == 0 || fingerprints.Any(value => value.Length != 64 || !value.All(Uri.IsHexDigit)))
            throw new InvalidOperationException("Configure Beskedfordeler client certificate SHA-256 fingerprints.");
        var directory = builder.Configuration["Beskedfordeler:SchemaDirectory"] ??
            Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Beskedfordeler", "Schemas");
        builder.Services.AddSingleton(new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas(directory)));
        var organizations = builder.Configuration.GetSection("Beskedfordeler:Organizations").Get<Dictionary<string, Guid>>() ?? [];
        builder.Services.AddSingleton<IBeskedfordelerDeletionMapper>(new OrgBrugerDeletionMapper(organizations));
        builder.Services.AddSingleton<IBeskedfordelerClientCertificateValidator, BeskedfordelerClientCertificateValidator>();
        builder.Services.AddScoped<BeskedfordelerJob>();
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

    public static void MapBeskedfordeler(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Beskedfordeler:Enabled"))
            app.MapPost("/api/v2/integration/beskedfordeler", (Func<HttpContext, Task<IResult>>)BeskedfordelerEndpoint.Receive)
                .AllowAnonymous().ExcludeFromDescription().ShortCircuit();
    }
}
