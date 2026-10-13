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

namespace Infrastructure.STS.Beskedfordeler;

public class BeskedfordelerJob(BeskedfordelerXmlReader reader,
    IBeskedfordelerDeletionMapper mapper, ExternalUserChangeIngestionService ingestion)
{
    public const string QueueName = "beskedfordeler";

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
