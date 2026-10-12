using System.Diagnostics.Metrics;
using RabbitMQ.Client;

namespace PubSub.Application.Api.UserSync;

/// <summary>Dedicated connection; channels are isolated so concurrent callbacks cannot interleave publishes.</summary>
public sealed class UserChangePublisher(IConnectionFactory factory, UserChangeTopology topology) : IUserChangePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    internal static readonly Meter Meter = new("Kitos.PubSub.UserSync", "1.0");
    private static readonly Counter<long> Confirmed = Meter.CreateCounter<long>("user_sync.publish.confirmed");
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>("user_sync.publish.failed");

    public async Task Publish(ReadOnlyMemory<byte> body, string messageId, int attempt, bool deadLetter, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var connection = await GetConnection(timeout.Token);
            await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true), timeout.Token);
            await topology.Declare(channel, timeout.Token);
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = UserChangeContract.Version,
                MessageId = messageId,
                CorrelationId = messageId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?> { ["user-sync-attempt"] = Math.Clamp(attempt, 0, 8) }
            };
            var queue = deadLetter ? topology.DeadLetter : attempt > 0 ? topology.Retry(attempt) : topology.Main;
            // Client 7 confirmation tracking throws for both basic.nack and mandatory basic.return.
            await channel.BasicPublishAsync("", queue, true, properties, body, timeout.Token);
            Confirmed.Add(1);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            Failures.Add(1);
            throw new UserChangePublishException(ex);
        }
    }

    private async Task<IConnection> GetConnection(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            if (_connection != null) await _connection.DisposeAsync();
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally { _connectionLock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null) await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }
}
