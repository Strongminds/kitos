using Core.ApplicationServices.Users;
using Infrastructure.STS.Beskedfordeler;
using Microsoft.Extensions.Logging;

namespace Presentation.Web.Infrastructure.Beskedfordeler;

// Preserve the serialized type identity of Hangfire jobs queued before the infrastructure move.
public sealed class BeskedfordelerJob(BeskedfordelerXmlReader reader,
    IBeskedfordelerDeletionMapper mapper, ExternalUserChangeIngestionService ingestion,
    ILogger<global::Infrastructure.STS.Beskedfordeler.BeskedfordelerJob> logger)
    : global::Infrastructure.STS.Beskedfordeler.BeskedfordelerJob(reader, mapper, ingestion, logger);
