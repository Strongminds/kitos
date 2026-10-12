using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PubSub.Infrastructure.DataAccess;
using RabbitMQ.Client;

namespace PubSub.Application.Api.UserSync;

public sealed class UserChangeHealthCheck(IServiceScopeFactory scopes, IConnectionFactory factory,
    UserChangeTopology topology, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("UserSync:Enabled")) return HealthCheckResult.Healthy("Disabled");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PubSubContext>();
            var pending = await db.UserChangeReceipts.CountAsync(x => x.AcceptedAt == null, timeout.Token);
            var oldest = await db.UserChangeReceipts.Where(x => x.AcceptedAt == null)
                .Select(x => (DateTime?)x.ReservedAt).MinAsync(timeout.Token);
            await using var connection = await factory.CreateConnectionAsync(timeout.Token);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: timeout.Token);
            var main = await channel.QueueDeclarePassiveAsync(topology.Main, timeout.Token);
            var dead = await channel.QueueDeclarePassiveAsync(topology.DeadLetter, timeout.Token);
            var data = new Dictionary<string, object>
            {
                ["pendingReceipts"] = pending,
                ["oldestPendingAgeSeconds"] = oldest.HasValue ? (DateTime.UtcNow - oldest.Value).TotalSeconds : 0,
                ["queueDepth"] = main.MessageCount,
                ["consumers"] = main.ConsumerCount,
                ["deadLetters"] = dead.MessageCount
            };
            for (var attempt = 1; attempt <= 8; attempt++)
                data["retryDepth" + attempt] = (await channel.QueueDeclarePassiveAsync(topology.Retry(attempt), timeout.Token)).MessageCount;
            return pending > 0 || dead.MessageCount > 0 || main.ConsumerCount == 0
                ? HealthCheckResult.Degraded("User sync requires operator attention.", data: data)
                : HealthCheckResult.Healthy("User sync connected.", data);
        }
        catch (Exception)
        {
            // No connection strings, certificate details or event data in health output.
            return HealthCheckResult.Unhealthy("User sync receipt store or broker unavailable.");
        }
    }
}
