using Infrastructure.STS.Beskedfordeler;
using Microsoft.AspNetCore.Hosting;

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

        var enabled = builder.Configuration.GetValue<bool>("Beskedfordeler:Enabled");
        var fingerprints = builder.Configuration.GetSection("Beskedfordeler:ClientCertificateSha256").Get<string[]>() ?? [];
        if (enabled && (fingerprints.Length == 0 || fingerprints.Any(value => value.Length != 64 || !value.All(Uri.IsHexDigit))))
            throw new InvalidOperationException("Configure Beskedfordeler client certificate SHA-256 fingerprints.");
        var directory = builder.Configuration["Beskedfordeler:SchemaDirectory"] ??
            Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Beskedfordeler", "Schemas");
        if (enabled)
            builder.Services.AddSingleton(new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas(directory)));
        else
            builder.Services.AddSingleton(_ => new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas(directory)));
        builder.Services.AddScoped<BeskedfordelerService>();
        builder.Services.AddSingleton<IBeskedfordelerClientCertificateValidator, BeskedfordelerClientCertificateValidator>();
        if (!enabled) return;
        var organizations = builder.Configuration.GetSection("Beskedfordeler:Organizations").Get<Dictionary<string, Guid>>() ?? [];
        builder.Services.AddSingleton<IBeskedfordelerDeletionMapper>(new OrgBrugerDeletionMapper(organizations));

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
