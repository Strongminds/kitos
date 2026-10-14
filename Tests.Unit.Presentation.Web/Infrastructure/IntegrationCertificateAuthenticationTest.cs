using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Infrastructure.STS.Beskedfordeler;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Presentation.Web.Controllers.API.V2.Integration;
using Presentation.Web.Infrastructure.Authentication;
using Presentation.Web.Infrastructure.Configuration;
using Presentation.Web.Infrastructure.Middleware;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure;

public class IntegrationCertificateAuthenticationTest
{
    [Fact]
    public async Task HttpsHandshake_ProvidesClientCertificate_AndCertificateIsRequired()
    {
        using var serverKey = RSA.Create(2048);
        using var clientKey = RSA.Create(2048);
        var serverRequest = new CertificateRequest("CN=localhost", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var clientRequest = new CertificateRequest("CN=integration-client", clientKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var generatedServerCertificate = serverRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var generatedClientCertificate = clientRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        // Schannel needs imported private keys for TLS; no trust-store installation is required.
        using var serverCertificate = X509CertificateLoader.LoadPkcs12(generatedServerCertificate.Export(X509ContentType.Pfx), null);
        using var clientCertificate = X509CertificateLoader.LoadPkcs12(generatedClientCertificate.Export(X509ContentType.Pfx), null);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, endpoint => endpoint.UseHttps(https =>
        {
            https.ServerCertificate = serverCertificate;
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            // Pin the generated test certificate instead of changing the machine's trust store.
            https.ClientCertificateValidation = (certificate, _, _) => certificate.Thumbprint == clientCertificate.Thumbprint;
        })));
        builder.Services.AddAuthentication(IntegrationCertificateAuthenticationHandler.SchemeName)
            .AddScheme<IntegrationCertificateAuthenticationOptions, IntegrationCertificateAuthenticationHandler>(
                IntegrationCertificateAuthenticationHandler.SchemeName, options =>
                {
                    options.ValidateCertificate = certificate => certificate.Thumbprint == clientCertificate.Thumbprint;
                });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(BeskedfordelerV2Controller).Assembly);
        builder.Services.AddSingleton(Mock.Of<IBackgroundJobClient>(MockBehavior.Strict));
        builder.Services.AddSingleton(new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas()));
        builder.Services.AddScoped<BeskedfordelerService>();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        var url = app.Urls.Single() + "/api/v2/integration/beskedfordeler";

        foreach (var sendCertificate in new[] { false, true })
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, certificate, _, _) => certificate?.Thumbprint == serverCertificate.Thumbprint
            };
            if (sendCertificate) handler.ClientCertificates.Add(clientCertificate);
            using var client = new HttpClient(handler);
            using var response = await client.PostAsync(url, null);
            Assert.Equal(sendCertificate ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData(true, false, false, false, 403)]
    [InlineData(true, false, false, true, 403)]
    [InlineData(true, true, false, false, 403)]
    [InlineData(true, true, false, true, 403)]
    [InlineData(false, true, true, false, 403)]
    [InlineData(true, true, true, false, 200)]
    [InlineData(true, true, true, true, 200)]
    public async Task Integration_RequiresHttpsAndCertificate_IgnoresBearer(bool https,
        bool present, bool trusted, bool bearer, int status)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=integration-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:SecurityKeyString"] = new string('a', 64)
        });
        builder.Services.AddKitosAuthentication(builder.Configuration);
        builder.Services.AddAuthentication().AddScheme<IntegrationCertificateAuthenticationOptions, IntegrationCertificateAuthenticationHandler>(
            IntegrationCertificateAuthenticationHandler.SchemeName, options =>
            {
                options.ValidateCertificate = received => trusted && received.Thumbprint == certificate.Thumbprint;
            });
        var bearerCalled = false;
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            options.Events.OnMessageReceived = _ => { bearerCalled = true; return Task.CompletedTask; });
        builder.Services.AddKitosMvc();
        builder.Services.AddControllers().AddApplicationPart(typeof(BeskedfordelerV2Controller).Assembly);
        builder.Services.AddScoped<CorrelationIdMiddleware>();
        // No user context or user middleware services: resolving them for an integration request must fail this test.
        builder.Services.AddSingleton(Mock.Of<IBackgroundJobClient>(MockBehavior.Strict));
        builder.Services.AddSingleton(new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas()));
        builder.Services.AddScoped<BeskedfordelerService>();
        await using var app = builder.Build();
        app.UseRouting();
        app.UseKitosApiSecurity();
        app.MapControllers();
        app.MapGet("/ordinary", () => "requires a user").RequireAuthorization();
        await app.StartAsync();

        var response = await app.GetTestServer().SendAsync(context =>
        {
            context.Request.Method = "POST";
            context.Request.Path = "/api/v2/integration/beskedfordeler";
            context.Request.Scheme = https ? "https" : "http";
            if (present) context.Connection.ClientCertificate = certificate;
            if (bearer) context.Request.Headers.Authorization = "Bearer ignored-token";
            // A forwarded certificate header is never proof of a TLS handshake.
            context.Request.Headers["X-ARR-ClientCert"] = Convert.ToBase64String(certificate.RawData);
        });
        Assert.Equal(status, response.Response.StatusCode);
        Assert.False(bearerCalled);
        if (status == 200)
        {
            Assert.Equal(IntegrationCertificateAuthenticationHandler.SchemeName, response.User.Identity!.AuthenticationType);
            Assert.StartsWith("application/xml", response.Response.ContentType);
        }

        using var ordinaryRequest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/ordinary");
        ordinaryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        using var ordinaryResponse = await app.GetTestClient().SendAsync(ordinaryRequest);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, ordinaryResponse.StatusCode);
        Assert.True(bearerCalled);
    }
}
