using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using UserSync.Transport;

namespace Presentation.Web.Infrastructure.UserSync;

public sealed class UserSyncHealthCheck(IConnectionFactory factory, UserChangeTopology topology) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await using var connection = await factory.CreateConnectionAsync(timeout.Token);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: timeout.Token);
            var main = await channel.QueueDeclarePassiveAsync(topology.Main, timeout.Token);
            var dead = await channel.QueueDeclarePassiveAsync(topology.DeadLetter, timeout.Token);
            var data = new Dictionary<string, object>
            {
                ["queueDepth"] = main.MessageCount,
                ["consumers"] = main.ConsumerCount,
                ["deadLetters"] = dead.MessageCount
            };
            for (var attempt = 1; attempt <= 8; attempt++)
                data["retryDepth" + attempt] = (await channel.QueueDeclarePassiveAsync(topology.Retry(attempt), timeout.Token)).MessageCount;
            return main.ConsumerCount == 0 || dead.MessageCount > 0
                ? HealthCheckResult.Degraded("User sync requires attention.", data: data)
                : HealthCheckResult.Healthy("User sync consumer connected.", data);
        }
        catch (Exception) { return HealthCheckResult.Unhealthy("User sync broker unavailable."); }
    }
}
