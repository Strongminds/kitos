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

        builder.Configuration["AppSettings:BeskedfordelerCertificateSha256"] = new string('a', 64);

        builder.AddBeskedfordeler();

        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IHostedService));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IBeskedfordelerDeletionMapper));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(global::Infrastructure.STS.Beskedfordeler.BeskedfordelerJob));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(BeskedfordelerService));
    }
}
