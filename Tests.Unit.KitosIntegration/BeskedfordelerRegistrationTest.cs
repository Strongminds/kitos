using System;
using KitosIntegration.Infrastructure.Beskedfordeler;
using KitosIntegration.Services;
using Kitos.Integration.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using KitosIntegration.Configuration;
using Xunit;

namespace Tests.Unit.KitosIntegration;

public class BeskedfordelerRegistrationTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddBeskedfordelerReceiver_MissingHangfireConnection_Throws(string? connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["AppSettings:BeskedfordelerCertificateThumbprint"] = new string('a', 40);
        builder.Configuration["ConnectionStrings:kitos_HangfireDB"] = connectionString;

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddBeskedfordelerReceiver());

        Assert.Contains("kitos_HangfireDB", exception.Message);
    }

    [Fact]
    public void AddBeskedfordeler_RotationThumbprints_RegistersCallback()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["AppSettings:BeskedfordelerCertificateThumbprint"] =
            $" {new string('a', 40)} , {new string('B', 40)} ";

        builder.Configuration["ConnectionStrings:kitos_HangfireDB"] = "Host=localhost;Database=hangfire";
        builder.AddBeskedfordelerReceiver();

        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IBeskedfordelerClientCertificateValidator));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IBeskedfordelerService));
        builder.Services.AddSingleton<Hangfire.IBackgroundJobClient>(new Moq.Mock<Hangfire.IBackgroundJobClient>().Object);
        using var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.IsType<BeskedfordelerService>(scope.ServiceProvider.GetRequiredService<IBeskedfordelerService>());
        Assert.IsType<BeskedfordelerXmlReader>(scope.ServiceProvider.GetRequiredService<IBeskedfordelerXmlReader>());
        Assert.IsType<HangfireBeskedfordelerJobRepository>(scope.ServiceProvider.GetRequiredService<IBeskedfordelerJobRepository>());
        Assert.DoesNotContain(provider.GetServices<IHostedService>(), service => service is Hangfire.BackgroundJobServerHostedService);
        Assert.DoesNotContain(builder.Services, service => service.ServiceType == typeof(IBeskedfordelerJob));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" , ")]
    [InlineData("ABC")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAG")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA,invalid")]
    public void AddBeskedfordeler_InvalidThumbprints_Throws(string? thumbprints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["AppSettings:BeskedfordelerCertificateThumbprint"] = thumbprints;

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddBeskedfordelerReceiver());

        Assert.Contains("40 hexadecimal characters", exception.Message);
    }
}

