using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Authentication;

namespace PubSub.Application.Api.UserSync.Beskedfordeler;

public static class BeskedfordelerRegistration
{
    public static void AddBeskedfordeler(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IBeskedfordelerClientCertificateValidator, BeskedfordelerClientCertificateValidator>();
        if (!builder.Configuration.GetValue<bool>("Beskedfordeler:Enabled")) return;
        if (!builder.Configuration.GetValue<bool>("UserSync:Enabled"))
            throw new InvalidOperationException("Beskedfordeler requires UserSync:Enabled.");
        var fingerprints = builder.Configuration.GetSection("Beskedfordeler:ClientCertificateSha256").Get<string[]>() ?? [];
        if (fingerprints.Length == 0 || fingerprints.Any(value => value.Length != 64 || !value.All(Uri.IsHexDigit)))
            throw new InvalidOperationException("Configure environment-specific Beskedfordeler client certificate SHA-256 fingerprints.");
        var schemaDirectory = builder.Configuration["Beskedfordeler:SchemaDirectory"];
        if (string.IsNullOrWhiteSpace(schemaDirectory))
            throw new InvalidOperationException("Beskedfordeler requires a complete local SchemaDirectory.");
        var schemas = BeskedfordelerXmlReader.LoadSchemas(schemaDirectory);
        builder.Services.AddSingleton(new BeskedfordelerXmlReader(schemas));
        builder.Services.AddScoped<IBeskedfordelerDeletionMapper, UnsupportedBeskedfordelerDeletionMapper>();
        builder.Services.AddScoped<BeskedfordelerReceiver>();
        builder.WebHost.ConfigureKestrel(options => options.ConfigureHttpsDefaults(https =>
        {
            // Other PubSub routes still accept bearer authentication; the callback requires a certificate.
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        }));
    }
}
