using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Core.ApplicationServices.Users;
using Microsoft.Extensions.Logging;

namespace Infrastructure.STS.Beskedfordeler;

public class BeskedfordelerJob(BeskedfordelerXmlReader reader,
    IBeskedfordelerDeletionMapper mapper, ExternalUserChangeIngestionService ingestion,
    ILogger<BeskedfordelerJob> logger)
{
    public const string QueueName = "beskedfordeler";
    public static readonly Guid TestMessageType = new("7654895c-a3f3-4c77-8afd-4c77330f963c");

    [Queue(QueueName)]
    [AutomaticRetry(Attempts = 8, DelaysInSeconds = new[] { 2, 4, 8, 16, 32, 64, 128, 256 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [BeskedfordelerRetention]
    public async Task Process(string originalMessage, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(originalMessage));
        XElement input;
        try { input = await reader.Read(stream, cancellationToken); }
        catch (Exception ex) when (ex is XmlException or XmlSchemaException)
        {
            // Hangfire logs job exceptions; parser messages can disclose original XML.
            throw new InvalidOperationException("Stored Beskedfordeler XML failed validation.");
        }
        var message = input.Element(BeskedfordelerResponse.Envelope + "Haendelsesbesked");
        var id = message?.Element(BeskedfordelerResponse.Envelope + "BeskedId")?
            .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")?.Value;
        if (!Guid.TryParse(id, out var uuid) || uuid == Guid.Empty ||
            message?.Element(BeskedfordelerResponse.Envelope + "BeskedVersion")?.Value != "1.0")
            throw new InvalidOperationException("Invalid Beskedfordeler envelope.");
        var messageType = message.Element(BeskedfordelerResponse.Envelope + "Beskedkuvert")?
            .Element(BeskedfordelerResponse.Envelope + "Filtreringsdata")?
            .Element(BeskedfordelerResponse.Envelope + "Beskedtype")?
            .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")?.Value;
        if (Guid.TryParse(messageType, out var type) && type == TestMessageType)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Warning is intentional: test delivery must be visible on servers filtering out Information.
            // Log interpreted identifiers only, never arbitrary XML or decoded payload content.
            logger.LogWarning("Received KOMBIT Test/TestbeskedType1 message {MessageId} of type {MessageType} (version 1.0).", uuid, type);
            return;
        }
        var mapped = mapper.Map(input) ?? throw new InvalidOperationException("Unsupported Beskedfordeler event or municipality mapping.");
        cancellationToken.ThrowIfCancellationRequested();
        var result = ingestion.Ingest(mapped with { ExternalMessageId = uuid.ToString("D") });
        if (!result.Ok)
            throw new InvalidOperationException($"Beskedfordeler persistence failed: {result.Error.FailureType}.");
    }
}

// Failed jobs do not expire. Successful payloads remain recoverable for seven days.
public sealed class BeskedfordelerRetentionAttribute : JobFilterAttribute, IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is SucceededState) context.JobExpirationTimeout = TimeSpan.FromDays(7);
    }
    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }
}
