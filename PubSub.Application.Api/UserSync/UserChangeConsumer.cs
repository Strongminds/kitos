using System.Diagnostics.Metrics;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PubSub.Application.Api.UserSync;

public sealed class UserChangeConsumer(IConnectionFactory factory, UserChangeTopology topology,
    IUserChangePublisher publisher, KitosUserChangeDelivery delivery, IConfiguration configuration,
    ILogger<UserChangeConsumer> logger) : BackgroundService
{
    private static readonly Counter<long> Retries = UserChangePublisher.Meter.CreateCounter<long>("user_sync.delivery.retry");
    private static readonly Counter<long> DeadLetters = UserChangePublisher.Meter.CreateCounter<long>("user_sync.delivery.dead_letter");
    private static readonly Counter<long> Accepted = UserChangePublisher.Meter.CreateCounter<long>("user_sync.delivery.accepted");
    private static readonly Histogram<double> Age = UserChangePublisher.Meter.CreateHistogram<double>("user_sync.delivery.age", "s");
    private static readonly UpDownCounter<long> Connections = UserChangePublisher.Meter.CreateUpDownCounter<long>("user_sync.consumer.connected");

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (configuration.GetValue<bool>("UserSync:Enabled")) delivery.ValidateConfiguration();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("UserSync:Enabled")) return;
        var endpoint = delivery.ValidateConfiguration();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Explicit session replacement on shutdown also handles broker cancellation and channel errors.
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(false, false, consumerDispatchConcurrency: 1), stoppingToken);
                var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                channel.ChannelShutdownAsync += (_, _) => { ended.TrySetResult(); return Task.CompletedTask; };
                channel.CallbackExceptionAsync += (_, _) => { ended.TrySetResult(); return Task.CompletedTask; };
                await topology.Declare(channel, stoppingToken);
                await channel.BasicQosAsync(0, 1, false, stoppingToken);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.UnregisteredAsync += (_, _) => { ended.TrySetResult(); return Task.CompletedTask; };
                consumer.ReceivedAsync += async (_, message) =>
                {
                    try { await Handle(channel, message, endpoint, stoppingToken); }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { ended.TrySetResult(); }
                    catch (Exception)
                    {
                        // Keep the original unacked until the failed session closes; back off before reconnecting.
                        logger.LogWarning("User sync delivery/transfer failed; original remains unacknowledged.");
                        ended.TrySetResult();
                    }
                };
                await channel.BasicConsumeAsync(topology.Main, false, consumer, stoppingToken);
                Connections.Add(1);
                try { await ended.Task.WaitAsync(stoppingToken); }
                finally { Connections.Add(-1); }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("User sync consumer session unavailable; reconnecting."); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    public async Task Handle(IChannel channel, BasicDeliverEventArgs message, Uri endpoint, CancellationToken cancellationToken)
    {
        var body = message.Body.ToArray();
        var id = message.BasicProperties.MessageId ?? string.Empty;
        var deadLetter = false;
        var success = false;
        try
        {
            var normalized = UserChangeContract.Read(body);
            if (message.BasicProperties.Type != UserChangeContract.Version || normalized.ExternalMessageId != id)
                throw new ArgumentException("Invalid queue contract metadata.");
            var status = await delivery.Send(endpoint, body, cancellationToken);
            success = (int)status is >= 200 and < 300;
            deadLetter = KitosUserChangeDelivery.IsPermanentFailure(status);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException) { deadLetter = true; }
        catch (HttpRequestException) { /* Authentication and network failures remain retryable. */ }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }

        if (!success)
        {
            var attempt = message.BasicProperties.Headers?.TryGetValue("user-sync-attempt", out var value) == true && value is int count
                ? Math.Clamp(count, 0, 8) : 0;
            await publisher.Publish(body, id, Math.Min(attempt + 1, 8), deadLetter, cancellationToken);
            if (deadLetter) DeadLetters.Add(1); else Retries.Add(1);
            logger.LogWarning("User sync message {MessageId} transferred to {Destination}", id, deadLetter ? "dead letter" : "retry");
        }
        else Accepted.Add(1);
        if (message.BasicProperties.Timestamp.UnixTime > 0)
            Age.Record(Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - message.BasicProperties.Timestamp.UnixTime));
        await channel.BasicAckAsync(message.DeliveryTag, false, cancellationToken);
    }
}
