using System;
using System.Linq;
using System.Security.Authentication;
using Hangfire;
using Hangfire.PostgreSql;
using KitosIntegration.Infrastructure.Beskedfordeler;
using KitosIntegration.Services;
using KitosIntegration.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KitosIntegration.Configuration;

public static class BeskedfordelerRegistration
{
    public static void AddBeskedfordelerReceiver(this WebApplicationBuilder builder)
    {
        var thumbprints = BeskedfordelerClientCertificateValidator.GetAllowedThumbprints(builder.Configuration);
        if (thumbprints.Length == 0 || thumbprints.Any(value => value.Length != 40 || !value.All(Uri.IsHexDigit)))
            throw new InvalidOperationException("Configure Beskedfordeler client certificate thumbprints (40 hexadecimal characters each).");
        var connectionString = builder.Configuration.GetConnectionString("kitos_HangfireDB");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("kitos_HangfireDB connection string is required");

        var directory = builder.Configuration["Beskedfordeler:SchemaDirectory"];
        builder.Services.AddSingleton<IBeskedfordelerXmlReader>(new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas(directory)));
        builder.Services.AddScoped<IBeskedfordelerService, BeskedfordelerService>();

        builder.Services.AddScoped<IBeskedfordelerJobRepository, HangfireBeskedfordelerJobRepository>();
        builder.Services.AddSingleton<IBeskedfordelerClientCertificateValidator, BeskedfordelerClientCertificateValidator>();
        builder.Services.AddAuthentication(IntegrationCertificateAuthenticationHandler.SchemeName)
            .AddScheme<IntegrationCertificateAuthenticationOptions, IntegrationCertificateAuthenticationHandler>(
                IntegrationCertificateAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddOptions<IntegrationCertificateAuthenticationOptions>(IntegrationCertificateAuthenticationHandler.SchemeName)
            .Configure<IBeskedfordelerClientCertificateValidator>((options, validator) => options.ValidateCertificate = validator.IsValid);
        builder.Services.AddAuthorization();

        // KITOS owns storage initialization and consumes jobs. This host only submits them.
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                PrepareSchemaIfNecessary = false,
                SchemaName = "hangfire"
            }));

        builder.WebHost.ConfigureKestrel(options => options.ConfigureHttpsDefaults(https =>
        {
            // Missing certificates reach authentication and receive the existing HTTP 403 response.
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        }));
    }
}

