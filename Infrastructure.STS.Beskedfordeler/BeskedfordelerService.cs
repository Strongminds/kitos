using System;
using System.IO;

using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Hangfire;

namespace Infrastructure.STS.Beskedfordeler;

public sealed class BeskedfordelerService(BeskedfordelerXmlReader reader, IBackgroundJobClient client)
{
    public async Task<BeskedfordelerStatus> Receive(Stream requestBody, CancellationToken cancellation)
    {
        try
        {
            // Bound bytes even for chunked requests and hosts without a request-size feature.
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await requestBody.ReadAsync(buffer, cancellation)) > 0)
            {
                if (body.Length + count > BeskedfordelerXmlReader.MaxBodySize) return BeskedfordelerStatus.CannotReceive;
                await body.WriteAsync(buffer.AsMemory(0, count), cancellation);
            }
            body.Position = 0;
            var input = await reader.Read(body, cancellation);
            var message = input.Element(BeskedfordelerResponse.Envelope + "Haendelsesbesked");
            var id = message?.Element(BeskedfordelerResponse.Envelope + "BeskedId")?
                .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")?.Value;
            if (!Guid.TryParse(id, out var uuid) || uuid == Guid.Empty) return BeskedfordelerStatus.CannotReceive;
            if (message?.Element(BeskedfordelerResponse.Envelope + "BeskedVersion")?.Value != "1.0")
                return BeskedfordelerStatus.UnsupportedVersion;
            // Preserve the exact bytes, including the XML encoding declaration, in Hangfire only.
            var original = Convert.ToBase64String(body.ToArray());
            var jobId = client.Enqueue<BeskedfordelerJob>(job => job.Process(original, CancellationToken.None));
            if (string.IsNullOrWhiteSpace(jobId)) return BeskedfordelerStatus.Unavailable;
            return BeskedfordelerStatus.Accepted;
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

}
