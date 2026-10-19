using System;
using Infrastructure.STS.Beskedfordeler;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Presentation.Web.Infrastructure.Beskedfordeler;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure;

public class BeskedfordelerRegistrationTest
{
    [Fact]
    public void AddBeskedfordeler_RegistersWorkerAndCallback()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration["AppSettings:BeskedfordelerCertificateThumbprint"] = new string('a', 40);

        builder.AddBeskedfordeler();

        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IHostedService));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IBeskedfordelerDeletionMapper));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(global::Infrastructure.STS.Beskedfordeler.BeskedfordelerJob));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(BeskedfordelerService));
    }

    [Fact]
    public void AddBeskedfordeler_RotationThumbprints_RegistersCallback()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["AppSettings:BeskedfordelerCertificateThumbprint"] =
            $" {new string('a', 40)} , {new string('B', 40)} ";

        builder.AddBeskedfordeler();

        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IBeskedfordelerClientCertificateValidator));
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

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddBeskedfordeler());

        Assert.Contains("40 hexadecimal characters", exception.Message);
    }
}
