using System.Security.Cryptography.X509Certificates;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Tools.Beskedfordeler;

public static class BeskedfordelerSender
{
    public const string Exchange = "AFSEND_BESKED_EXCHANGE";
    public const string ReplyQueue = "amq.rabbitmq.reply-to";

    public static ConnectionFactory CreateConnectionFactory(string host, X509Certificate2 certificate, string token) =>
        new()
        {
            HostName = host,
            Port = 5671,
            VirtualHost = "BF",
            AutomaticRecoveryEnabled = false,
            AuthMechanisms = [new TokenSaslMechanism(token)],
            Ssl = new SslOption
            {
                Enabled = true,
                ServerName = host,
                Certs = new X509CertificateCollection { certificate },
                CheckCertificateRevocation = true
            }
        };

    public static async Task<int> Publish(IChannel channel, string token, string message,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("D");
        var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) =>
        {
            if (args.BasicProperties.CorrelationId == correlationId)
                response.TrySetResult(Encoding.UTF8.GetString(args.Body.Span));
            return Task.CompletedTask;
        };
        channel.BasicReturnAsync += OnReturn;
        channel.ChannelShutdownAsync += OnShutdown;
        try
        {
            await channel.BasicConsumeAsync(ReplyQueue, autoAck: true, consumer, cancellationToken);
            var properties = new BasicProperties
            {
                CorrelationId = correlationId,
                MessageId = Guid.NewGuid().ToString("D"),
                ReplyTo = ReplyQueue,
                ContentType = "application/xml",
                ContentEncoding = "utf-8",
                Headers = new Dictionary<string, object?> { ["token"] = token }
            };
            await channel.BasicPublishAsync(Exchange, "", mandatory: true, properties,
                Encoding.UTF8.GetBytes(message), cancellationToken);
            return TestMessage.ReadStatus(await response.Task.WaitAsync(cancellationToken));
        }
        finally
        {
            channel.BasicReturnAsync -= OnReturn;
            channel.ChannelShutdownAsync -= OnShutdown;
        }

        Task OnReturn(object sender, BasicReturnEventArgs args)
        {
            response.TrySetException(new InvalidOperationException("KOMBIT returned the unroutable publication."));
            return Task.CompletedTask;
        }

        Task OnShutdown(object sender, ShutdownEventArgs args)
        {
            response.TrySetException(new InvalidOperationException("AMQP channel closed before the KOMBIT response."));
            return Task.CompletedTask;
        }
    }
}
