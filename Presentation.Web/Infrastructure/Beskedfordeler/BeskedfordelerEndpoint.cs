using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Web.Infrastructure.Beskedfordeler;

public static class BeskedfordelerEndpoint
{
    public static async Task<IResult> Receive(HttpContext context)
    {
        var cancellation = context.RequestAborted;
        var certificates = context.RequestServices.GetRequiredService<IBeskedfordelerClientCertificateValidator>();
        if (!context.Request.IsHttps || !certificates.IsValid(await context.Connection.GetClientCertificateAsync(cancellation)))
            return Results.StatusCode(403);
        var type = context.Request.ContentType?.Split(';')[0].Trim();
        if (!string.Equals(type, "application/xml", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "text/xml", StringComparison.OrdinalIgnoreCase)) return Reply(BeskedfordelerStatus.CannotReceive);
        try
        {
            // Bound bytes even for chunked requests and hosts without a request-size feature.
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await context.Request.Body.ReadAsync(buffer, cancellation)) > 0)
            {
                if (body.Length + count > BeskedfordelerXmlReader.MaxBodySize) return Reply(BeskedfordelerStatus.CannotReceive);
                await body.WriteAsync(buffer.AsMemory(0, count), cancellation);
            }
            body.Position = 0;
            var reader = context.RequestServices.GetRequiredService<BeskedfordelerXmlReader>();
            var input = await reader.Read(body, cancellation);
            var message = input.Element(BeskedfordelerResponse.Envelope + "Haendelsesbesked");
            var id = message?.Element(BeskedfordelerResponse.Envelope + "BeskedId")?
                .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")?.Value;
            if (!Guid.TryParse(id, out var uuid) || uuid == Guid.Empty) return Reply(BeskedfordelerStatus.CannotReceive);
            if (message?.Element(BeskedfordelerResponse.Envelope + "BeskedVersion")?.Value != "1.0")
                return Reply(BeskedfordelerStatus.UnsupportedVersion);
            // Preserve the exact bytes, including the XML encoding declaration, in Hangfire only.
            var original = Convert.ToBase64String(body.ToArray());
            var client = context.RequestServices.GetRequiredService<IBackgroundJobClient>();
            var jobId = client.Enqueue<BeskedfordelerJob>(job => job.Process(original, CancellationToken.None));
            if (string.IsNullOrWhiteSpace(jobId)) return Reply(BeskedfordelerStatus.Unavailable);
            return Reply(BeskedfordelerStatus.Accepted);
        }
        catch (Exception ex) when (ex is XmlException or XmlSchemaException or FormatException)
        {
            return Reply(BeskedfordelerStatus.CannotReceive);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            // Never log XML or parser diagnostics. A failed enqueue must not acknowledge acceptance.
            return Reply(BeskedfordelerStatus.Unavailable);
        }
    }

    private static IResult Reply(BeskedfordelerStatus status) =>
        Results.Content(BeskedfordelerResponse.Serialize(status), "application/xml", Encoding.UTF8, 200);
}
