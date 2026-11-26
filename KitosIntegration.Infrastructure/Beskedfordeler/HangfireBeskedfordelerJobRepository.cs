using System;
using System.Threading;
using Hangfire;
using Kitos.Integration.Contracts;


namespace KitosIntegration.Infrastructure.Beskedfordeler;

public sealed class HangfireBeskedfordelerJobRepository(IBackgroundJobClient client) : IBeskedfordelerJobRepository
{
    public string? Enqueue(byte[] originalMessage)
    {
        // Preserve the exact bytes, including the XML encoding declaration, in durable storage.
        var original = Convert.ToBase64String(originalMessage);
        return client.Enqueue<IBeskedfordelerJob>(job => job.Process(original, CancellationToken.None));
    }
}

