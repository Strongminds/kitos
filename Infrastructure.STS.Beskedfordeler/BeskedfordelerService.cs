using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.STS.Beskedfordeler;

public sealed class BeskedfordelerService(BeskedfordelerXmlReader reader, IBackgroundJobClient client,
    ILogger<BeskedfordelerService> logger)
{
    private const int ReadBufferSize = 8192;
    private const string SupportedVersion = "1.0";

    // Async is required: ASP.NET Core disallows synchronous reads of the request body.
    public async Task<BeskedfordelerStatus> Receive(Stream requestBody, CancellationToken cancellation)
    {
        try
        {
            var body = await ReadBoundedBody(requestBody, cancellation);
            if (body == null) return BeskedfordelerStatus.CannotReceive;

            var message = (await reader.Read(new MemoryStream(body), cancellation))
                .Element(BeskedfordelerResponse.Envelope + "Haendelsesbesked");

            if (!HasValidMessageId(message)) return BeskedfordelerStatus.CannotReceive;
            if (!IsSupportedVersion(message)) return BeskedfordelerStatus.UnsupportedVersion;

            var messageType = message!.Element(BeskedfordelerResponse.Envelope + "Beskedkuvert")?
                .Element(BeskedfordelerResponse.Envelope + "Filtreringsdata")?
                .Element(BeskedfordelerResponse.Envelope + "Beskedtype")?
                .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")?.Value;
            if (Guid.TryParse(messageType, out var type) && type == BeskedfordelerJob.TestMessageType)
            {
                cancellation.ThrowIfCancellationRequested();
                var messageId = Guid.Parse(message.Element(BeskedfordelerResponse.Envelope + "BeskedId")!
                    .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")!.Value);
                // Connectivity probes are logged immediately; only business events require durable processing.
                logger.LogWarning("Received KOMBIT Test/TestbeskedType1 message {MessageId} of type {MessageType} (version 1.0).", messageId, type);
                return BeskedfordelerStatus.Accepted;
            }

            return EnqueueForProcessing(body) ? BeskedfordelerStatus.Accepted : BeskedfordelerStatus.Unavailable;
        }
        catch (Exception ex) when (ex is XmlException or XmlSchemaException or FormatException)
        {
            return BeskedfordelerStatus.CannotReceive;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            // Never log XML or parser diagnostics. A failed enqueue must not acknowledge acceptance.
            return BeskedfordelerStatus.Unavailable;
        }
    }

    // Bound bytes even for chunked requests and hosts without a request-size feature. Returns null when too large.
    private static async Task<byte[]?> ReadBoundedBody(Stream requestBody, CancellationToken cancellation)
    {
        using var body = new MemoryStream();
        var buffer = new byte[ReadBufferSize];
        int count;
        while ((count = await requestBody.ReadAsync(buffer, cancellation)) > 0)
        {
            if (body.Length + count > BeskedfordelerXmlReader.MaxBodySize) return null;
            await body.WriteAsync(buffer.AsMemory(0, count), cancellation);
        }
        return body.ToArray();
    }

    private static bool HasValidMessageId(XElement? message)
    {
        var id = message?.Element(BeskedfordelerResponse.Envelope + "BeskedId")?
            .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")?.Value;
        return Guid.TryParse(id, out var uuid) && uuid != Guid.Empty;
    }

    private static bool IsSupportedVersion(XElement? message) =>
        message?.Element(BeskedfordelerResponse.Envelope + "BeskedVersion")?.Value == SupportedVersion;

    // Preserve the exact bytes, including the XML encoding declaration, in Hangfire only.
    private bool EnqueueForProcessing(byte[] body)
    {
        var original = Convert.ToBase64String(body);
        var jobId = client.Enqueue<BeskedfordelerJob>(job => job.Process(original, CancellationToken.None));
        return !string.IsNullOrWhiteSpace(jobId);
    }
}
