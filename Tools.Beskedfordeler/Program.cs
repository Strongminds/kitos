using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml.Linq;
using Infrastructure.STS.Common.Model.Token;
using Kombit.InfrastructureSamples.Token;
using RabbitMQ.Client;

namespace Tools.Beskedfordeler;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args is ["--help"] or ["-h"] || args.Length is < 3 or > 4)
        {
            Console.WriteLine("Publish one KOMBIT Test/TestbeskedType1 message.");
            Console.WriteLine("Usage: dotnet run --project Tools.Beskedfordeler -- <sts-config.json> <sender-uuid> <municipality-cvr> [amqp-host]");
            Console.WriteLine("Default host: beskedfordeler.eksterntest-stoettesystemerne.dk");
            return args is ["--help"] or ["-h"] ? 0 : 1;
        }

        var stage = "configuration";
        try
        {
            var senderId = Guid.Parse(args[1]);
            var messageId = Guid.NewGuid();
            var message = TestMessage.Create(messageId, senderId, args[2], DateTimeOffset.UtcNow);
            var config = JsonSerializer.Deserialize<SenderStsConfiguration>(await File.ReadAllTextAsync(args[0]),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("STS configuration is required.");
            var host = args.Length == 4 ? args[3] : "beskedfordeler.eksterntest-stoettesystemerne.dk";
            if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
                throw new ArgumentException("Expected an AMQP hostname without scheme or port.");

            stage = "client certificate loading";
            using var certificate = CertificateLoader.LoadCertificateWithFallback(
                config.SsoCertFilePath, config.SsoCertPassword,
                StoreName.My, StoreLocation.LocalMachine, config.ClientCertificateThumbprint);
            if (!certificate.HasPrivateKey)
                throw new InvalidOperationException("Client certificate requires a private key.");

            stage = "STS token issuance";
            Console.WriteLine("Requesting a BeskedAfsend STS token...");
            var issuedToken = (GenericXmlSecurityToken)new TokenFetcher(config.ToTokenFetcherConfig())
                .IssueToken("http://entityid.kombit.dk/service/bfo_afsend/2", args[2]);
            var token = issuedToken.TokenXml.OuterXml;

            stage = "AMQP publication";
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var factory = BeskedfordelerSender.CreateConnectionFactory(host, certificate, token);
            await using var connection = await factory.CreateConnectionAsync(timeout.Token);
            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true), timeout.Token);
            Console.WriteLine($"Publishing message {messageId:D} to {host}...");
            var response = await BeskedfordelerSender.Publish(channel, token,
                message.ToString(SaveOptions.DisableFormatting), timeout.Token);
            Console.WriteLine("KOMBIT response:");
            Console.WriteLine(response);
            var status = TestMessage.ReadStatus(response);
            Console.WriteLine($"KOMBIT status: {status}. Message UUID: {messageId:D}.");
            if (status != 20)
            {
                Console.Error.WriteLine("KOMBIT rejected the message. Status 41 means the STS token expired.");
                return 1;
            }
            Console.WriteLine("Accepted by KOMBIT. Callback delivery must be verified separately.");
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or IOException or JsonException
            or InvalidOperationException or CryptographicException or TimeoutException
            or OperationCanceledException or System.ServiceModel.CommunicationException
            or RabbitMQ.Client.Exceptions.RabbitMQClientException or System.Xml.XmlException)
        {
            // Integration exceptions may contain tokens or wire payloads; never print them.
            Console.Error.WriteLine($"Failed during {stage} ({ex.GetType().Name}). No success was confirmed.");
            Console.Error.WriteLine($"Error chain: {DescribeFailure(ex)}");
            return 1;
        }
    }

    public static string DescribeFailure(Exception error)
    {
        var details = new List<string>();
        Collect(error);
        return string.Join(" -> ", details);

        void Collect(Exception current)
        {
            details.Add(current is System.Net.Sockets.SocketException socket
                ? $"{current.GetType().Name} ({socket.SocketErrorCode})"
                : current.GetType().Name);
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                    Collect(inner);
            }
            else if (current.InnerException is { } inner)
                Collect(inner);
        }
    }
}
