using Kitos.Integration.Contracts;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Users;
using Core.ApplicationServices.Users;
using Microsoft.Extensions.Logging;

namespace Infrastructure.STS.Beskedfordeler;

public class BeskedfordelerJob(BeskedfordelerXmlReader reader,
    IBeskedfordelerDeletionMapper mapper, ExternalUserChangeIngestionService ingestion,
    ILogger<BeskedfordelerJob> logger) : IBeskedfordelerJob
{
    public const string QueueName = IBeskedfordelerJob.QueueName;
    public static readonly Guid TestMessageType = IBeskedfordelerJob.TestMessageType;
    private const string SupportedVersion = "1.0";

    [Queue(QueueName)]
    [AutomaticRetry(Attempts = 8, DelaysInSeconds = new[] { 2, 4, 8, 16, 32, 64, 128, 256 }, OnAttemptsExceeded = AttemptsExceededAction.Fail, ExceptOn = new[] { typeof(BeskedfordelerQuarantineException) })]
    [BeskedfordelerRetention]
    public async Task Process(string originalMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = await ReadStoredMessage(originalMessage, cancellationToken);
        // Hangfire requires a faulted task to retain a Failed job and apply its retry policy.
        await input.Bind(value => ProcessMessage(value, cancellationToken))
            .Match(_ => Task.CompletedTask, error => Task.FromException(ToHangfireException(error)));
    }

    private Result<Guid, OperationError> ProcessMessage(XElement input, CancellationToken cancellationToken)
    {
        var message = input.Element(BeskedfordelerMessage.Envelope + "Haendelsesbesked");
        return ReadMessageId(message).Bind(uuid => HandleTestMessage(message, uuid, cancellationToken)
            ? Result<Guid, OperationError>.Success(uuid)
            : MapEvent(input, uuid, cancellationToken).Bind(mapped => Ingest(mapped, uuid, cancellationToken)));
    }

    private async Task<Result<XElement, OperationError>> ReadStoredMessage(string originalMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new MemoryStream(Convert.FromBase64String(originalMessage));
            return await reader.Read(stream, cancellationToken);
        }
        catch (Exception ex) when (ex is XmlException or XmlSchemaException or FormatException)
        {
            // Never attach parser diagnostics: they may disclose original XML in Hangfire.
            return new OperationError("InvalidStoredPayload", OperationFailure.BadInput);
        }
    }

    private static Result<Guid, OperationError> ReadMessageId(XElement? message)
    {
        var id = message?.Element(BeskedfordelerMessage.Envelope + "BeskedId")?
            .Element(BeskedfordelerMessage.Sagdok + "UUIDIdentifikator")?.Value;
        if (!Guid.TryParse(id, out var uuid) || uuid == Guid.Empty ||
            message?.Element(BeskedfordelerMessage.Envelope + "BeskedVersion")?.Value != SupportedVersion)
            return new OperationError("InvalidEnvelope", OperationFailure.BadInput);
        return uuid;
    }

    private bool HandleTestMessage(XElement? message, Guid uuid, CancellationToken cancellationToken)
    {
        var typeIdentifier = message?.Element(BeskedfordelerMessage.Envelope + "Beskedkuvert")?
            .Element(BeskedfordelerMessage.Envelope + "Filtreringsdata")?
            .Element(BeskedfordelerMessage.Envelope + "Beskedtype")?
            .Element(BeskedfordelerMessage.Sagdok + "UUIDIdentifikator")?.Value;
        if (!Guid.TryParse(typeIdentifier, out var type) || type != TestMessageType) return false;
        cancellationToken.ThrowIfCancellationRequested();
        // Warning keeps connectivity probes visible on servers filtering out Information.
        logger.LogWarning("Received KOMBIT Test/TestbeskedType1 message {MessageId} of type {MessageType} (version 1.0).", uuid, type);
        return true;
    }

    private Result<ExternalUserChangeInput, OperationError> MapEvent(XElement input, Guid uuid, CancellationToken cancellationToken)
    {
        var result = mapper.Map(input);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Match<Result<ExternalUserChangeInput, OperationError>>(
            mapped => mapped with { ExternalMessageId = uuid.ToString("D") },
            failure => new OperationError($"Mapping/{failure}; MessageId={uuid:D}", OperationFailure.BadInput));
    }

    private Result<Guid, OperationError> Ingest(ExternalUserChangeInput input, Guid uuid, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ingestion.Ingest(input).Match<Result<Guid, OperationError>>(
            _ => uuid,
            error => new OperationError($"Ingestion/{error.FailureType}; MessageId={uuid:D}", error.FailureType));
    }

    private static Exception ToHangfireException(OperationError error) => error.FailureType switch
    {
        OperationFailure.BadInput or OperationFailure.NotFound or OperationFailure.Forbidden or
            OperationFailure.Conflict or OperationFailure.BadState => new BeskedfordelerQuarantineException(error.Message.Match(reason => reason, () => error.FailureType.ToString())),
        _ => new InvalidOperationException($"Beskedfordeler persistence failed: {error.FailureType}.")
    };
}
