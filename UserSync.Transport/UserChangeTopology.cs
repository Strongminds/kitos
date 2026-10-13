using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace UserSync.Transport;

public sealed class UserChangeTopology(IConfiguration configuration)
{
    public string Main { get; } = configuration["UserSync:QueuePrefix"] ?? "kitos.user-sync.v1";
    public string DeadLetter => Main + ".dead";
    public string Retry(int attempt) => Main + ".retry." + Math.Clamp(attempt, 1, 8);
    public static int DelaySeconds(int attempt) => 1 << Math.Clamp(attempt, 1, 8);

    public async Task Declare(IChannel channel, CancellationToken cancellationToken)
    {
        await DeclareQueue(channel, Main, QueueArguments(), cancellationToken);
        await DeclareQueue(channel, DeadLetter, QueueArguments(), cancellationToken);
        for (var attempt = 1; attempt <= 8; attempt++)
        {
            var arguments = QueueArguments();
            arguments["x-message-ttl"] = DelaySeconds(attempt) * 1000;
            arguments["x-dead-letter-exchange"] = "";
            arguments["x-dead-letter-routing-key"] = Main;
            arguments["x-dead-letter-strategy"] = "at-least-once";
            await DeclareQueue(channel, Retry(attempt), arguments, cancellationToken);
        }
    }

    private static Dictionary<string, object?> QueueArguments() => new()
    {
        ["x-queue-type"] = "quorum",
        ["x-overflow"] = "reject-publish",
        // RabbitMQ 4 defaults to a delivery limit. Never discard on repeated consumer crashes.
        ["x-delivery-limit"] = -1
    };

    private static Task<QueueDeclareOk> DeclareQueue(IChannel channel, string name, Dictionary<string, object?> arguments,
        CancellationToken cancellationToken) => channel.QueueDeclareAsync(name, true, false, false, arguments,
            cancellationToken: cancellationToken);
}
