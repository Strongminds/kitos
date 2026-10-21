using Kitos.Integration.Contracts;
using KitosIntegration.Infrastructure.Beskedfordeler;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;

namespace KitosIntegration.Services;

public sealed class BeskedfordelerService(IBeskedfordelerXmlReader reader, IBeskedfordelerJobRepository jobs,
    ILogger<BeskedfordelerService> logger) : IBeskedfordelerService
{
    private const int ReadBufferSize = 8192;
    private const string SupportedVersion = "1.0";

    // Async is required: ASP.NET Core disallows synchronous reads of the request body.
    public async Task<BeskedfordelerStatus> Receive(Stream requestBody, CancellationToken cancellation, string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString("D");
        var stage = "read request body";
        logger.LogWarning("Beskedfordeler receive service invoked. TraceId {TraceId}.", traceId);
        try
        {
            logger.LogWarning("Beskedfordeler reading request body. TraceId {TraceId}, MaxBytes {MaxBytes}.",
                traceId, BeskedfordelerXmlReader.MaxBodySize);
            var body = await ReadBoundedBody(requestBody, cancellation);
            if (body == null)
            {
                logger.LogWarning("Beskedfordeler request exceeds body limit. TraceId {TraceId}, StatusKode {StatusKode}.",
                    traceId, (int)BeskedfordelerStatus.CannotReceive);
                return BeskedfordelerStatus.CannotReceive;
            }
            logger.LogWarning("Beskedfordeler request body read. TraceId {TraceId}, BytesRead {BytesRead}.", traceId, body.Length);

            stage = "validate XML";
            logger.LogWarning("Beskedfordeler validating XML against schemas. TraceId {TraceId}.", traceId);
            using var stream = new MemoryStream(body);
            var message = (await reader.Read(stream, cancellation))
                .Element(BeskedfordelerMessage.Envelope + "Haendelsesbesked");
            logger.LogWarning("Beskedfordeler XML schema validation completed. TraceId {TraceId}.", traceId);

            stage = "validate envelope";
            if (!HasValidMessageId(message))
            {
                logger.LogWarning("Beskedfordeler message ID is missing or invalid. TraceId {TraceId}, StatusKode {StatusKode}.",
                    traceId, (int)BeskedfordelerStatus.CannotReceive);
                return BeskedfordelerStatus.CannotReceive;
            }
            var messageId = Guid.Parse(message!.Element(BeskedfordelerMessage.Envelope + "BeskedId")!
                .Element(BeskedfordelerMessage.Sagdok + "UUIDIdentifikator")!.Value);
            logger.LogWarning("Beskedfordeler message ID validated. TraceId {TraceId}, MessageId {MessageId}.", traceId, messageId);
            if (!IsSupportedVersion(message))
            {
                logger.LogWarning("Beskedfordeler envelope version unsupported. TraceId {TraceId}, StatusKode {StatusKode}.",
                    traceId, (int)BeskedfordelerStatus.UnsupportedVersion);
                return BeskedfordelerStatus.UnsupportedVersion;
            }
            logger.LogWarning("Beskedfordeler envelope version validated. TraceId {TraceId}.", traceId);

            stage = "classify message";
            var messageType = message!.Element(BeskedfordelerMessage.Envelope + "Beskedkuvert")?
                .Element(BeskedfordelerMessage.Envelope + "Filtreringsdata")?
                .Element(BeskedfordelerMessage.Envelope + "Beskedtype")?
                .Element(BeskedfordelerMessage.Sagdok + "UUIDIdentifikator")?.Value;
            if (Guid.TryParse(messageType, out var type) && type == IBeskedfordelerJob.TestMessageType)
            {
                cancellation.ThrowIfCancellationRequested();
                // Connectivity probes are logged immediately; only business events require durable processing.
                logger.LogWarning("Received KOMBIT Test/TestbeskedType1 message {MessageId} of type {MessageType} (version 1.0).", messageId, type);
                logger.LogWarning("Beskedfordeler test message accepted without enqueue. TraceId {TraceId}, MessageId {MessageId}, StatusKode {StatusKode}.",
                    traceId, messageId, (int)BeskedfordelerStatus.Accepted);
                return BeskedfordelerStatus.Accepted;
            }

            stage = "enqueue message";
            logger.LogWarning("Beskedfordeler enqueueing non-test message. TraceId {TraceId}, MessageId {MessageId}, Queue {Queue}.",
                traceId, messageId, IBeskedfordelerJob.QueueName);
            var status = EnqueueForProcessing(body, traceId) ? BeskedfordelerStatus.Accepted : BeskedfordelerStatus.Unavailable;
            logger.LogWarning("Beskedfordeler receive service completed. TraceId {TraceId}, MessageId {MessageId}, StatusKode {StatusKode}.",
                traceId, messageId, (int)status);
            return status;
        }
        catch (Exception ex) when (ex is XmlException or XmlSchemaException or FormatException)
        {
            logger.LogWarning("Beskedfordeler request rejected. TraceId {TraceId}, Stage {Stage}, ExceptionType {ExceptionType}, StatusKode {StatusKode}.",
                traceId, stage, ex.GetType().Name, (int)BeskedfordelerStatus.CannotReceive);
            return BeskedfordelerStatus.CannotReceive;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Beskedfordeler receive service cancelled. TraceId {TraceId}, Stage {Stage}.", traceId, stage);
            throw;
        }
        catch (Exception ex)
        {
            // Never log XML or parser diagnostics. A failed enqueue must not acknowledge acceptance.
            logger.LogWarning("Beskedfordeler receive service failed. TraceId {TraceId}, Stage {Stage}, ExceptionType {ExceptionType}, StatusKode {StatusKode}.",
                traceId, stage, ex.GetType().Name, (int)BeskedfordelerStatus.Unavailable);
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
        var id = message?.Element(BeskedfordelerMessage.Envelope + "BeskedId")?
            .Element(BeskedfordelerMessage.Sagdok + "UUIDIdentifikator")?.Value;
        return Guid.TryParse(id, out var uuid) && uuid != Guid.Empty;
    }

    private static bool IsSupportedVersion(XElement? message) =>
        message?.Element(BeskedfordelerMessage.Envelope + "BeskedVersion")?.Value == SupportedVersion;

    private bool EnqueueForProcessing(byte[] body, string traceId)
    {
        var jobId = jobs.Enqueue(body);
        if (string.IsNullOrWhiteSpace(jobId))
            logger.LogWarning("Beskedfordeler enqueue failed: no job ID returned. TraceId {TraceId}.", traceId);
        else
            logger.LogWarning("Beskedfordeler durable enqueue completed. TraceId {TraceId}, JobId {JobId}.", traceId, jobId);
        return !string.IsNullOrWhiteSpace(jobId);
    }
}

