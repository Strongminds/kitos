using Kitos.Integration.Contracts;
using Hangfire;
using Infrastructure.STS.Beskedfordeler;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Web.Infrastructure.Beskedfordeler;

public static class BeskedfordelerRegistration
{
    public static void AddBeskedfordelerWorker(this WebApplicationBuilder builder)
    {
        var directory = builder.Configuration["Beskedfordeler:SchemaDirectory"];
        builder.Services.AddSingleton(new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas(directory)));
        builder.Services.AddScoped<IBeskedfordelerDeletionMapper, OrgBrugerDeletionMapper>();
        // Preserve both serialized job identities, including deliveries queued before the extraction.
        builder.Services.AddScoped<BeskedfordelerJob>();
        builder.Services.AddScoped<global::Infrastructure.STS.Beskedfordeler.BeskedfordelerJob>();
        builder.Services.AddScoped<IBeskedfordelerJob, global::Infrastructure.STS.Beskedfordeler.BeskedfordelerJob>();
        builder.Services.AddHangfireServer(options =>
        {
            options.Queues = [BeskedfordelerJob.QueueName];
            options.WorkerCount = 2;
        });
    }
}
