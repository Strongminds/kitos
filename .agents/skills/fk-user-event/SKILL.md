---
name: fk-user-event
description: Investigate or change KITOS FK Organisation user events delivered through Beskedfordeler, including mapping, durable delivery, retries, replay, and administrator review. Use for this inbound flow rather than general KOMBIT organization synchronization.
---

# FK user events through Beskedfordeler

Paths below are relative to the repository root. Read `.agents/instructions.md` first. Verify current code: older architecture notes and the broader KOMBIT skill contain historical receiver placement and unmatched-user behavior.

## Locate the affected boundary

- `KitosIntegration/Controllers/BeskedfordelerV2Controller.cs` and `KitosIntegration.Services/BeskedfordelerService.cs`: standalone callback, bounded XML validation, envelope checks, protocol status, and durable handoff. The callback is no longer hosted in Presentation.Web.
- `KitosIntegration/Configuration/BeskedfordelerRegistration.cs` and `KitosIntegration.Infrastructure/Beskedfordeler/`: certificate authentication, schemas, and Hangfire client. This host submits jobs; it does not run the worker.
- `Kitos.Integration.Contracts/IBeskedfordelerJob.cs`: serialized contract, queue, test-message identifier, and retry attributes.
- `Presentation.Web/Infrastructure/Beskedfordeler/BeskedfordelerRegistration.cs`: KITOS worker registration. The adjacent `BeskedfordelerJob.cs` preserves the old concrete job identity.
- `Infrastructure.STS.Beskedfordeler/BeskedfordelerJob.cs`, `OrgBrugerDeletionMapper.cs`, and `BeskedfordelerRetentionAttribute.cs`: processing, event/municipality mapping, and retention.
- `Core.ApplicationServices/Users/ExternalUserChange*`: ingestion, reading, and administrator resolution. Follow `IExternalUserChangeRepository` for persistence and concurrency.
- `docs/technical-documentation/architecture-decision-log/fk-org-users-integration.md` and `KitosIntegration/README.md`: protocol and deployment background. Check statements against code before treating them as requirements.

## Preserve integration invariants

Business-event acceptance follows durable enqueue of exact original bytes. The receiver currently logs and accepts test messages without enqueue; the worker also handles historical queued test messages. Preserve serialized type/method identities when moving jobs.

Keep certificate authentication on the standalone receiver, bounded reads, schema validation, and prohibited DTD/external XML resolution. Log interpreted identifiers and reasons, not XML or payload-bearing parser diagnostics.

Map the affected user from `ObjektRegistrering/ObjektId`, not the actor in `Beskeddata/Bruger`. Read supported message/object/action UUIDs from the mapper. The current mapper requires one registration, the supported deactivation action, valid user UUID/CVR, matching allowed recipient, and an explicitly zoned timestamp. Resolve CVR against exactly one currently opted-in organization (`FkOrgUsersConnected`) on each attempt; do not add an appsettings municipality map.

Ingestion creates a Pending administrator-reviewed change; receipt does not remove access. Currently an unmatched SSO identity or missing organization User role returns NotFound rather than storing an unmatched pending item, despite older documentation.

Replay uses `BeskedId` as `ExternalMessageId`. Equivalent events are idempotent; reuse for different content conflicts. Preserve microsecond timestamp normalization and previously resolved status. Inspect signup checks before assuming replay bypasses them. Keep PubSub outside this flow unless explicitly in scope.

## Investigate repeating failures

Distinguish source redelivery from execution retries. Correlate message ID, receive trace ID, Hangfire job ID, retry count, and state history using authorized operational evidence. Repository inspection alone cannot establish a deployed loop's cause.

The current interface and implementation declare eight retries at 2, 4, 8, 16, 32, 64, 128, and 256 seconds, then Failed. Inspect effective filters and deployed version if observations differ. Failed jobs remain retained; successful payloads have seven-day retention. Retention is not retry scheduling.

The receiver enqueues each accepted non-test delivery; ingestion deduplication happens later. Multiple jobs for one message can therefore be repeated delivery, including a lost acknowledgement. A failure before ingestion never reaches ingestion deduplication. Check worker recovery/restarts and operator or external requeues when supported by state history; do not assume a code loop.

The mapper returns `Result<ExternalUserChangeInput, BeskedfordelerMappingFailure>` with a specific reason. Trace individual rejection reasons: unsupported type/action, registration count, invalid identifiers/recipient/timestamp, or missing/ambiguous opted-in municipality. Ingestion failures are a separate stage and can also be deterministic.

When asked to improve handling, prefer explicit mapping outcomes over undifferentiated null:

- Intentionally irrelevant events can complete with a recorded ignored reason.
- Unsupported/malformed events and unresolved municipality mappings should stay visible and replayable without automatic retries that cannot resolve the cause.
- Temporary infrastructure failures should retain bounded retries.

Quarantine is implemented as retained Hangfire Failed jobs carrying `BeskedfordelerQuarantineException`. The interface and concrete job retry attributes exclude that exception through `ExceptOn`; other failures retain bounded retries. Operators correct the cause and use Retry in the protected Hangfire dashboard to replay the original payload. Keep all serialized job identities covered by retry-policy tests. Preserve payload and reason before completing terminal handling, and define retention/replay semantics. Do not swallow unknown events or disable retries for every failure. Replay operational jobs only within the user's authorized scope; stop when the same unresolved cause recurs.

## Validate affected behavior

Use existing suites matching the changed boundary:

- `Tests.Unit.KitosIntegration/BeskedfordelerTest.cs`: receiver, durable handoff, and job filters.
- `Tests.Unit.Presentation.Web/Infrastructure/BeskedfordelerWorkerTest.cs` and `Tests.Unit.Presentation.Web/Infrastructure/BeskedfordelerRegistrationTest.cs`: mapping, processing, and serialized job compatibility.
- `Tests.Unit.Core.ApplicationServices/ApplicationServices/Users/ExternalUserChangeIngestionServiceTest.cs` and adjacent read/resolution tests: signup, identity, replay, and review.
- `Tests.Container/Tests/BeskedfordelerHangfireRecoveryTest.cs`: real storage recovery; locate `UserSyncPersistenceTest` for ingestion persistence/concurrency.

For retry-policy changes, verify terminal failures stop scheduling, transient failures retry and exhaust, corrected mappings can replay, and duplicate delivery cannot create duplicate pending changes. Distinguish local checks from deployed IIS/certificate/KOMBIT acceptance. Follow the repository quality gate for code changes; skill-only edits need structural and reference validation.
