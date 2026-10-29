# FK Organization user integration

## Status and flow

Updated 2026-10-21: Beskedfordeler calls the standalone KitosIntegration API. KitosIntegration authenticates the sender certificate, validates the bounded XML envelope and durably enqueues the original bytes in the same PostgreSQL Hangfire storage as KITOS. The dedicated worker interprets immediate deactivation and preserves a pending `ExternalUserChange`. Receipt and processing never remove access. PubSub code, configuration, contracts and infrastructure are unchanged and outside this flow.

```text
Beskedfordeler -> KitosIntegration: POST /api/v2/integration/beskedfordeler
              -> certificate authorization + safe envelope validation
              -> durable Hangfire queue: beskedfordeler
              -> KITOS: scoped BeskedfordelerJob -> ExternalUserChangeIngestionService
              -> pending deletion -> local administrator apply/dismiss
```

KitosIntegration contains one thin, authorized controller at the unchanged route. KitosIntegration.Infrastructure configures certificate authentication and the Hangfire client; Controllers call the KitosIntegration.Services wrapper, which delegates to its own infrastructure receiver. Only Kitos.Integration.Contracts (message metadata, XSD schemas, statuses and the job interface) is shared with KITOS; service implementations and tests belong to their respective solutions. Infrastructure.STS.Beskedfordeler retains the KITOS worker implementation and old concrete job identities. Deploy the updated KITOS worker before the receiver, because new jobs use the interface contract. KITOS alone registers the worker, mapper and application services. KitosIntegration has no Hangfire server, dashboard, user authentication, user middleware or application database registration. The TLS handshake proves possession of the client private key; the scheme validates the certificate allowlist, chain, client-authentication usage, validity and revocation. Only HTTPS with an authorized certificate from Connection.GetClientCertificateAsync is accepted. Bearer tokens, cookies and arbitrary certificate headers cannot grant access. The callback has been removed from Presentation.Web.

The original XML bytes are Base64 encoded in Hangfire job arguments, preserving encoding declarations. There is no separate raw-message inbox or archive. Schema validation prohibits DTDs and external resolution, loads bundled local XSDs only, and limits requests to 1 MiB of bytes and XML characters, including chunked requests. The callback checks message UUID and envelope version 1.0; business interpretation occurs exclusively in the worker.

Protocol replies use HTTP 200 and `application/xml`: 20 after durable enqueue, 40 for invalid XML/envelope, 53 for failed handoff and 55 for unsupported envelope version. Missing/unauthorized certificates receive 403. The callback is registered only in KitosIntegration; the dedicated Hangfire worker is registered only in KITOS. The supplied SF1461 `AnvenderSystem.wadl` specifies HTTP 200 with XML status 20. Acceptance means durable handoff, not successful interpretation. Confirm that interpretation with Beskedfordeler in the target integration environment before activation. A lost HTTP reply may enqueue the same message again; processing is idempotent.

## Worker and preserved events

A dedicated Hangfire server consumes only `beskedfordeler`, with two workers. Hangfire's ASP.NET Core activator creates a dependency scope for each execution. It uses the existing PostgreSQL Hangfire connection and schema, leaving the default server and other job policies unchanged. Transient failures retain eight retries with delays of 2, 4, 8, 16, 32, 64, 128 and 256 seconds. Exhausted jobs remain Failed without automatic expiration. Typed mapping failures, invalid stored payloads/envelopes, and deterministic ingestion failures are quarantined immediately as retained Hangfire Failed jobs with a BeskedfordelerQuarantineException reason. AutomaticRetry excludes this exception on both the interface and concrete job identities; transient exceptions keep automatic retries. Cancellation before persistence leaves the job recoverable; interruption after commit is safe to replay.

`ExternalUserChangeIngestionService` validates organization opt-in and canonical event content. `ExternalUserChanges.ExternalMessageId` is unique. Competing inserts re-read the committed winner and compare organization, external user UUID, change type and UTC occurrence time normalized to microseconds. Equivalent events complete without resetting pending/applied/dismissed state. Conflicting content fails. An unmatched SSO identity or missing organization User role returns NotFound and quarantines the original job. Organization opt-in is required on every ingestion, including replay.

Successful jobs retain their original payload for seven days. Failed jobs are retained until an operator resolves or explicitly deletes them. Once a Hangfire job expires or is deleted, its original message cannot be replayed from KITOS; the interpreted event remains. Restrict database and local Hangfire dashboard access to authorized operators, protect backups and do not expose the dashboard publicly. Default dashboard local-access restrictions remain in place. Job arguments contain personal data; do not copy them into logs or tickets. Application replies do not include XML or parser diagnostics.
### Quarantine and operator replay

Quarantine uses the existing protected Hangfire dashboard's **Failed** list, not a separate queue or database table. Identify entries by `BeskedfordelerQuarantineException`; its safe reason includes the mapping/ingestion category and message ID when available. Original bytes remain in the job arguments and Failed jobs do not expire automatically.

1. Inspect the reason and correlate message ID with job ID. Multiple job IDs can represent repeated source deliveries; ingestion deduplication does not prevent enqueueing them.
2. Correct the underlying cause (municipality signup/CVR ambiguity, identity binding, or supported-event implementation). Do not change an event's identity to bypass a content conflict.
3. Use **Retry** on the retained Failed job. The queue attribute routes it to `beskedfordeler`; the worker re-reads the original payload and current organization signup state. Replaying equivalent ingested content preserves the existing review status.
4. Verify success and the expected pending/existing change. If the cause remains unresolved, the job returns directly to Failed without automatic retries. Do not repeatedly replay it without a corrective change.

Deploy the worker with the updated shared contracts before relying on this policy. Jobs already scheduled by the old policy will execute once more before being quarantined; new source deliveries can still create separate jobs. No automatic bulk replay or deletion is performed.

#### Users connection

The users connection is an on/off switch per organization, independent of the FK Organisation org-unit connection (`StsOrganizationConnection`); it does not require the org-unit connection. It is managed by local administrators (same permission as the org-unit connection) under `/api/v2/internal/organizations/{organizationUuid}/sts-organization-synchronization`:

| Operation | Route                       | Result                                                                                                                                                       |
| --------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `GET`     | `/users/connection-status`  | `accessStatus` (from the same FK Organisation access check as the org-unit connection), `connected`, `canCreateConnection`, `canDeleteConnection`, `connectedAt` (UTC). |
| `POST`    | `/users/connection`         | `204`; `400` if KITOS cannot access FK Organisation for the organization; `409` if already connected.                                                       |
| `DELETE`  | `/users/connection`         | `204`; `409` if not connected.                                                                                                                               |

Disconnecting does not delete existing `ExternalUserChanges` rows.

KITOS stores the event in `ExternalUserChanges` as `Pending`. It looks up the external user UUID through the SSO identity binding and associates a KITOS user only if that user currently has the `User` role in the stated organization. An unmatched identity quarantines the original Hangfire job rather than creating a pending review item. `ExternalMessageId` is unique. A retry with the same source ID and equivalent organization, identity, type, and normalized timestamp is idempotently accepted; an ID reused for a different event returns a conflict. Timestamp precision is normalized to database-supported microseconds before persistence/comparison.

### 3. Administrator review and resolution

The organization-scoped internal API is rooted at `/api/v2/internal/organization/{organizationUuid}/users/external-changes`. Every read and resolution operation checks that the caller is a local administrator of that organization.

| Operation | Route                   | Purpose                                                                                                                                         |
| --------- | ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET`     | `/pending-count`        | Read the number of unresolved changes.                                                                                                          |
| `GET`     | `/`                     | List and page through changes, optionally filtered by status/search, sorted by `receivedAt`, `status`, or `userName`; page size is at most 200. |
| `GET`     | `/{changeUuid}`         | Read one change.                                                                                                                                |
| `POST`    | `/{changeUuid}/apply`   | Apply a pending deletion.                                                                                                                       |
| `POST`    | `/{changeUuid}/dismiss` | Dismiss a pending deletion without changing access.                                                                                             |
| `POST`    | `/resolve`              | Apply or dismiss a batch of 1–100 selected changes, returning an individual success/error result for each.                                      |

The list includes the source message ID, external identity, matched user details when available, change type, status, occurrence and receipt times, and resolution details. Search covers the source message ID and matched user's name, last name, and email, with a maximum search length of 200 characters.

Resolution is performed in a serializable transaction, independently for each batch item. Before applying a deletion, KITOS re-resolves the external identity and verifies that it still belongs to a user with the `User` role in the target organization. If the event was associated with a different user at ingestion, or the binding/membership has changed, the operation conflicts. Global administrators cannot be removed through this local workflow. On apply, KITOS uses `IUserRightsService.RemoveAllRights` within the resolution transaction to remove organization roles and DPR, contract, system, and organization-unit rights in the affected organization, preserving grants in other organizations. Any removal failure rolls back the resolution rather than marking the event applied. If the user has no remaining `User` role in another organization, the KITOS account is soft-deleted and timestamped. The event becomes `Applied`, recording the resolving user and time. Dismissal leaves access unchanged and marks the event `Dismissed`. A resolved event cannot be resolved again. Successful application raises the administrative-access-rights-changed domain event.

## Configuration and deployment

| Setting | Requirement |
| --- | --- |
| `AppSettings:BeskedfordelerCertificateThumbprint` | Comma-separated list of standard 40-character SHA-1 certificate thumbprints (`X509Certificate2.Thumbprint`). Multiple entries allow rotation overlap; matching is case-insensitive. |
| `Beskedfordeler:SchemaDirectory` | Optional override for a complete external XSD directory. By default all schemas are embedded in Kitos.Integration.Contracts.dll; no schema directory or additional deployment artifact is required. |
| `ConnectionStrings:kitos_HangfireDB` | Existing durable PostgreSQL Hangfire storage. |

Use double underscores in environment variables, e.g. `AppSettings__BeskedfordelerCertificateThumbprint`. Configure the thumbprint allowlist on KitosIntegration, not Presentation.Web. The existing KITOS package preparation no longer installs callback certificate settings. Publish and configure KitosIntegration separately as described in KitosIntegration/README.md. The checked-in receiver settings are empty and must be supplied before startup. Restart after configuration or trust changes. Startup validates thumbprints and schemas. Organizations continue to sign up through KITOS; their stored CVR and FK Organisation users connection determine processing. No organization UUID mapping belongs in receiver settings.

The validator checks the sender thumbprint, certificate chain, validity, online revocation (excluding the root) and client-authentication usage. Install the full environment-specific trust chain and permit CRL/OCSP retrieval. Failure to establish trust or revocation status rejects the request. Kestrel requests optional client certificates and uses TLS 1.2/1.3; callback authentication is mandatory. KITOS no longer requests client certificates.

For IIS, configure the callback TLS boundary to negotiate client certificates (`SslNegotiateCert` with HTTPS) and verify that ASP.NET Core receives the authenticated certificate. Prefer supported IIS in-process hosting. Do not substitute an arbitrary forwarded-certificate header or an unreviewed TLS-terminating proxy. Use a separate IIS application/site for KitosIntegration; KITOS browser login is independent. The actual IIS handshake, revocation, rotation and upstream callback interoperability require environment acceptance tests; local unit tests do not certify that boundary.

Apply existing KITOS migrations `AddExternalUserChanges` and `AddFkOrgUsersConnection`. No new raw-message table or migration is needed. Local administrators must enable the users connection before delivery. Verify the signed-up organizations have correct CVRs in KITOS and verify a real event's FK Bruger UUID matches `SsoUserIdentity.ExternalUuid`.

For IIS workers install Application Initialization, set pool `startMode=AlwaysRunning`, enable application `preloadEnabled`, and disable idle shutdown. Test startup without browser traffic and recovery after recycling. Monitor the `beskedfordeler` queue age/count, active worker heartbeat, Scheduled retry jobs and Failed jobs through protected Hangfire administration. The obsolete RabbitMQ `/health/user-sync` endpoint and KITOS `UserSync:*` settings have been removed.

To replay: inspect the retained failure, correct availability/mapping/opt-in problems, and requeue the same job from Hangfire. Do not change the original message ID to bypass a conflict. Resolve conflicting IDs with the producer. Business failures receive protocol acceptance initially because interpretation is asynchronous; operators must monitor failed jobs. After successful-job expiry, obtain a source replay if needed.

`DeploymentScripts/TestExternalUserDeletion.ps1` enqueues a synthetic deletion directly in Hangfire using built KITOS assemblies and the application database SSO identity lookup. It bypasses HTTP and does not verify the receiver certificate handshake. Verify pending items through the internal API and inspect failed jobs separately. PubSub is outside this flow.

### Ordered cutover and rollback

These legacy-backlog steps apply only if an earlier experimental RabbitMQ version was deployed. Otherwise configure the direct callback and verify acceptance without a legacy migration.

1. Inventory deployed publishers, KITOS consumers, historical outbox/receipt state, all main/retry/dead queues and unacknowledged deliveries. Back up KITOS, Hangfire and broker state. This cannot be inferred from repository contents.
2. Pause the upstream user-change subscription. Keep the old KITOS consumer running until all accepted legacy deliveries are durably ingested. Account for pending receipt reservations, retry queues and dead letters; reconcile each source ID against KITOS. Do not purge queues or alter PubSub configuration/infrastructure as part of this implementation.
3. Resolve or preserve every outstanding message with a documented replay owner. If reconciliation cannot complete, postpone replacement; the new binary cannot drain legacy queues. Preserve the old executable/configuration for rollback.
4. Stop the old KITOS instance after reconciliation, deploy this version, configure certificates and confirm organization signups, enable the callback and validate worker health. Register the new callback URL with Beskedfordeler and resume the source subscription. Confirm a real event produces exactly one pending item and no access change.
5. For rollback, pause source delivery first. Drain or retain every accepted Hangfire job with its original payload; never discard Hangfire storage. Restore the old KITOS deployment and callback subscription only after reconciling accepted IDs and establishing how retained new jobs will be processed. Existing `ExternalUserChanges` provide idempotency across overlap. Do not claim a rollback complete while accepted jobs are stranded.

### Supported source interpretation

KOMBIT `Test/TestbeskedType1` (`7654895c-a3f3-4c77-8afd-4c77330f963c`) keeps its existing connectivity-probe behavior: after schema and envelope validation the receiver logs the message UUID/type and returns status 20 without enqueue. It requires certificate authentication but no organization signup. The worker retains support for previously queued probes. Business messages are acknowledged only after durable enqueue.

Only immediate `BrugerNedlagt` is supported. Subscribe to ORG_Bruger version 1.0 and the deactivation action UUID below, restricted to signed-up municipalities and one object registration. Planned deactivation is not supported; it needs effective-date scheduling and cancellation handling. Producer `Slet` and `Passiver` operations emit no events, so this is not comprehensive deletion reconciliation. Unit removal is not user deactivation.
| Source field under `Beskedkuvert/Filtreringsdata` | Mapping / validation |
| --- | --- |
| `Beskedtype/UUIDIdentifikator` | Must be `94ef3c82-10e9-4d12-a9d9-7a7242d5ba83` (`ORG_Bruger`). |
| `ObjektRegistrering/ObjektType/UUIDIdentifikator` | Must be `2b808769-8d09-4aa3-ac28-f8ac3526010f` (Bruger). |
| `ObjektRegistrering/ObjektHandling/UUIDIdentifikator` | Must be `9edb0c34-36e2-4d87-aba8-3ca7ade718ee` (`BrugerNedlagt`). |
| `ObjektRegistrering/ObjektId/UUIDIdentifikator` | Non-empty FK Bruger UUID becomes `ExternalUserUuid`. |
| `ObjektRegistrering/ObjektAnsvarligMyndighed/URNIdentifikator` | Responsible municipality CVR resolves against stored KITOS organizations with `FkOrgUsersConnected` enabled. Exactly one matching signup is required; missing or ambiguous matches fail processing. Each delivery/retry queries current signup state, so connection changes require no restart. |
| `TilladtModtager/URNIdentifikator` | The responsible municipality must appear in the delivered recipient list. Accept bare CVR, `urn:oio:cvr:` or `urn:oio:cvr-nr:`; never accept `*` as authorization. |
| `ObjektRegistrering/Registreringstidspunkt/TidsstempelDatoTid` | Required registration timestamp with explicit timezone, converted to UTC `OccurredAt`. This is registration time, not a future effective date. |

The existing SSO flow establishes the user-identity convention: `PrivilegeVerifiedState` passes the SAML UUID to `StsBrugerInfoService`, which reads FK Bruger by that UUID; `UserIdentifiedState` persists it as `SsoUserIdentity.ExternalUuid`. Confirm this with a real test user's event before activation. Never use a Person UUID, an actor UUID, or a CVR as the affected user's identity.

`Beskeddata` contains Base64 audit metadata (`Transport`, `Operation`, `TransactionID`, `Bruger`). Its `Bruger` is the actor making the change, not the affected user. The mapper does not decode or use it to decide who loses access. Exactly one `ObjektRegistrering` is supported per message; ambiguous multi-registration messages are rejected rather than partially acknowledged.

The agreed behavior is to translate immediate deactivation into a pending `Deleted` item for administrator review, preserving the existing apply/dismiss flow. No automatic access removal occurs at receipt.

## Validation and remaining acceptance checks

2026-10-13 local validation: both solution builds passed with zero warnings; the full unit suite passed, including 12 focused callback tests. Eight PostgreSQL persistence tests passed, including competing ingestion and replay after apply/dismiss. The PostgreSQL-backed Hangfire restart/failure/requeue test also passed. These storage tests exercise recovery and ingestion separately; they do not constitute an IIS-to-Beskedfordeler end-to-end acceptance test.

Unit tests cover certificate gating, durable enqueue failure/success, exact original payload retention, safe XML validation, replay and conflict behavior. Existing ingestion/review suites cover organization opt-in, identity matching, canonical timestamps and administrator resolution. `BeskedfordelerHangfireRecoveryTest` exercises PostgreSQL-backed Hangfire enqueue, database restart, retained failure and operator requeue; it requires Docker/Podman. `UserSyncPersistenceTest` exercises competing ingestion scopes and preserved apply/dismiss state against PostgreSQL. The target deployment boundary still requires integration acceptance.

Before production activation, demonstrate unauthorized, expired, revoked and rotated certificates through IIS; supported callback responses; duplicates and lost acknowledgements; concurrent workers; database outage and worker recycle; retry exhaustion and operator replay; retention cleanup leaving interpreted events intact; and lossless backlog reconciliation/rollback. Complete these environment checks before configuring production deliveries. Local quality-gate results are reported separately from deployment acceptance.

## References

- KOMBIT, _SF1500 Organisation_, integration version 3.3; nested `SF1500 Bilag v3.2/Beskeder/Beskedtype_ORG_Bruger_1.0.pdf`, sections 1-5 (the supplied 3.3 package retains a v3.2 directory name).
- KOMBIT, _SF1461 Modtag beskeder via Beskedfordeler_, integration version 2.0.
- KOMBIT, _D.09.02 Beskedfordeler-Besked-FåTilsendt-Snitflade_ (push interface).
- KOMBIT, _D.09.03 Beskedfordeler-Besked-Hent-Snitflade_ (pull alternative, not selected).
- KOMBIT, _B.08.09 Underbilag 2O - Beskedkuvert_ and `Beskedkuvert.xsd`.
- `AnvenderSystem.wadl` from SF1461 annexes confirms POST, application/xml and HTTP 200.
- `BeskeddataBase64ver.2.00.xsd`, `Beskedfordeler.wsdl`, and related operation/value-list schemas supplied with the integration documentation.
- Main implementation: `Infrastructure.STS.Beskedfordeler/`, `KitosIntegration/Controllers/BeskedfordelerV2Controller.cs`, `Core.ApplicationServices/Users/ExternalUserChange*`, and `Core.DomainModel/Users/ExternalUserChange.cs`.
