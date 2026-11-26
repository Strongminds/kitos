using Kitos.Integration.Contracts;
using System.Linq;
using System.Threading;
using Hangfire;
using Hangfire.Common;
using Infrastructure.STS.Beskedfordeler;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Web.Infrastructure.Beskedfordeler;
using Xunit;
using BeskedfordelerJob = Infrastructure.STS.Beskedfordeler.BeskedfordelerJob;

namespace Tests.Unit.Presentation.Web.Infrastructure;

public class BeskedfordelerRegistrationTest
{
    [Fact]
    public void AddBeskedfordelerWorker_RegistersWorkerWithoutCertificateConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddBeskedfordelerWorker();

        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IHostedService));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IBeskedfordelerDeletionMapper));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(global::Infrastructure.STS.Beskedfordeler.BeskedfordelerJob));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IBeskedfordelerJob)
            && service.ImplementationType == typeof(BeskedfordelerJob));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LegacyJob_CanDeserializeAndRetainsRetryAndQueuePolicies(bool presentationAssembly)
    {
        var legacyType = presentationAssembly
            ? typeof(global::Presentation.Web.Infrastructure.Beskedfordeler.BeskedfordelerJob)
            : typeof(BeskedfordelerJob);
        var method = legacyType.GetMethod(nameof(BeskedfordelerJob.Process))!;
        var original = new Job(legacyType, method, "cGF5bG9hZA==", CancellationToken.None);
        var restored = Hangfire.Storage.InvocationData.SerializeJob(original).DeserializeJob();

        Assert.Equal(legacyType, restored.Type);
        Assert.Equal(typeof(BeskedfordelerJob), restored.Method.DeclaringType);
        Assert.Equal("cGF5bG9hZA==", restored.Args[0]);
        Assert.Equal(BeskedfordelerJob.QueueName,
            Assert.Single(restored.Method.GetCustomAttributes(typeof(QueueAttribute), true).Cast<QueueAttribute>()).Queue);
        Assert.Equal(8,
            Assert.Single(restored.Method.GetCustomAttributes(typeof(AutomaticRetryAttribute), true).Cast<AutomaticRetryAttribute>()).Attempts);
    }
}
