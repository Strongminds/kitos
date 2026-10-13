# FK Organization user integration

## Status and flow

Implemented replacement (2026-10-13): Beskedfordeler calls KITOS directly. KITOS authenticates the sender certificate, validates the bounded XML envelope and durably enqueues the original bytes in Hangfire. The dedicated worker interprets immediate deactivation and preserves a pending `ExternalUserChange`. Receipt and processing never remove access. PubSub code, configuration, contracts and infrastructure are unchanged and outside this flow.

```text
Beskedfordeler -> POST /api/v2/integration/beskedfordeler
              -> certificate authorization + safe envelope validation
              -> durable Hangfire queue: beskedfordeler
              -> scoped BeskedfordelerJob -> ExternalUserChangeIngestionService
              -> pending deletion -> local administrator apply/dismiss
```

The endpoint is excluded from API descriptions. It runs at routing through an explicitly short-circuited endpoint, independent of ordinary login, user permission middleware and MVC filters. It accepts only HTTPS with an authorized client certificate from `Connection.GetClientCertificateAsync`; arbitrary certificate headers are not read. All other endpoints retain their authentication behavior.

The original XML bytes are Base64 encoded in Hangfire job arguments, preserving encoding declarations. There is no separate raw-message inbox or archive. Schema validation prohibits DTDs and external resolution, loads bundled local XSDs only, and limits requests to 1 MiB of bytes and XML characters, including chunked requests. The callback checks message UUID and envelope version 1.0; business interpretation occurs exclusively in the worker.

Protocol replies use HTTP 200 and `application/xml`: 20 after durable enqueue, 40 for invalid XML/envelope, 53 for failed handoff and 55 for unsupported envelope version. Missing/unauthorized certificates receive 403; the route is absent when disabled. The supplied SF1461 `AnvenderSystem.wadl` specifies HTTP 200 with XML status 20. Acceptance means durable handoff, not successful interpretation. Confirm that interpretation with Beskedfordeler in the target integration environment before activation. A lost HTTP reply may enqueue the same message again; processing is idempotent.

## Worker and preserved events

A dedicated Hangfire server consumes only `beskedfordeler`, with two workers. Hangfire's ASP.NET Core activator creates a dependency scope for each execution. It uses the existing PostgreSQL Hangfire connection and schema, leaving the default server and other job policies unchanged. Eight retries use delays of 2, 4, 8, 16, 32, 64, 128 and 256 seconds. Exhausted jobs remain Failed without automatic expiration. Unsupported events, unknown municipalities, disabled organization connections and conflicting IDs become visible failures rather than silently completing. Cancellation before persistence leaves the job recoverable; interruption after commit is safe to replay.

`ExternalUserChangeIngestionService` validates organization opt-in and canonical event content. `ExternalUserChanges.ExternalMessageId` is unique. Competing inserts re-read the committed winner and compare organization, external user UUID, change type and UTC occurrence time normalized to microseconds. Equivalent events complete without resetting pending/applied/dismissed state. Conflicting content fails. An unmatched SSO identity is preserved with no matched user. Organization opt-in is required on every ingestion, including replay.

Successful jobs retain their original payload for seven days. Failed jobs are retained until an operator resolves or explicitly deletes them. Once a Hangfire job expires or is deleted, its original message cannot be replayed from KITOS; the interpreted event remains. Restrict database and local Hangfire dashboard access to authorized operators, protect backups and do not expose the dashboard publicly. Default dashboard local-access restrictions remain in place. Job arguments contain personal data; do not copy them into logs or tickets. Application replies do not include XML or parser diagnostics.
#### Users connection

The users connection is an on/off switch per organization, independent of the FK Organisation org-unit connection (`StsOrganizationConnection`); it does not require the org-unit connection. It is managed by local administrators (same permission as the org-unit connection) under `/api/v2/internal/organizations/{organizationUuid}/sts-organization-synchronization`:

| Operation | Route                       | Result                                                                                                                                                       |
| --------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `GET`     | `/users/connection-status`  | `accessStatus` (from the same FK Organisation access check as the org-unit connection), `connected`, `canCreateConnection`, `canDeleteConnection`, `connectedAt` (UTC). |
| `POST`    | `/users/connection`         | `204`; `400` if KITOS cannot access FK Organisation for the organization; `409` if already connected.                                                       |
| `DELETE`  | `/users/connection`         | `204`; `409` if not connected.                                                                                                                               |

Disconnecting does not delete existing `ExternalUserChanges` rows.

KITOS stores the event in `ExternalUserChanges` as `Pending`. It looks up the external user UUID through the SSO identity binding and associates a KITOS user only if that user currently has the `User` role in the stated organization. An unmatched identity is still retained for administrator review. `ExternalMessageId` is unique. A retry with the same source ID and equivalent organization, identity, type, and normalized timestamp is idempotently accepted; an ID reused for a different event returns a conflict. Timestamp precision is normalized to database-supported microseconds before persistence/comparison.

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

Resolution is performed in a serializable transaction, independently for each batch item. Before applying a deletion, KITOS re-resolves the external identity and verifies that it still belongs to a user with the `User` role in the target organization. If the event was associated with a different user at ingestion, or the binding/membership has changed, the operation conflicts. Global administrators cannot be removed through this local workflow. On apply, KITOS removes that user's rights in the affected organization; if the user has no remaining `User` role in another organization, the KITOS account is soft-deleted and timestamped. The event becomes `Applied`, recording the resolving user and time. Dismissal leaves access unchanged and marks the event `Dismissed`. A resolved event cannot be resolved again. Successful application raises the administrative-access-rights-changed domain event.

## Configuration and deployment

| Setting | Requirement |
| --- | --- |
| `Beskedfordeler:Enabled` | Defaults false. Enables callback and dedicated workers. |
| `Beskedfordeler:ClientCertificateSha256` | Nonempty array of 64-character SHA-256 certificate fingerprints. Multiple entries allow rotation overlap. |
| `Beskedfordeler:Organizations` | Eight-digit CVR keys mapped to unique, nonempty KITOS organization UUIDs. |
| `Beskedfordeler:SchemaDirectory` | Optional; defaults to `Infrastructure/Beskedfordeler/Schemas` in output. Complete bundled XSDs are copied on build/publish. |
| `ConnectionStrings:kitos_HangfireDB` | Existing durable PostgreSQL Hangfire storage. |

Use double underscores in environment variables, e.g. `Beskedfordeler__ClientCertificateSha256__0` and `Beskedfordeler__Organizations__12345678`. Restart after configuration or trust changes. Enabled startup validates fingerprints, municipality configuration and schemas. No live municipality IDs, fingerprints or private keys are bundled.

The validator checks the sender fingerprint, certificate chain, validity, online revocation (excluding the root) and client-authentication usage. Install the full environment-specific trust chain and permit CRL/OCSP retrieval. Failure to establish trust or revocation status rejects the request. Kestrel requests optional client certificates and uses TLS 1.2/1.3; callback authentication is mandatory even though ordinary routes do not require certificates.

For IIS, configure the callback TLS boundary to negotiate client certificates (`SslNegotiateCert` with HTTPS) and verify that ASP.NET Core receives the authenticated certificate. Prefer supported IIS in-process hosting. Do not substitute an arbitrary forwarded-certificate header or an unreviewed TLS-terminating proxy. Configure certificate negotiation so ordinary browser login still works. The actual IIS handshake, revocation, rotation and upstream callback interoperability require environment acceptance tests; local unit tests do not certify that boundary.

Apply existing KITOS migrations `AddExternalUserChanges` and `AddFkOrgUsersConnection`. No new raw-message table or migration is needed. Local administrators must enable the users connection before delivery. Verify CVR mappings against KITOS records and verify a real event's FK Bruger UUID matches `SsoUserIdentity.ExternalUuid`.

For IIS workers install Application Initialization, set pool `startMode=AlwaysRunning`, enable application `preloadEnabled`, and disable idle shutdown. Test startup without browser traffic and recovery after recycling. Monitor the `beskedfordeler` queue age/count, active worker heartbeat, Scheduled retry jobs and Failed jobs through protected Hangfire administration. The obsolete RabbitMQ `/health/user-sync` endpoint and KITOS `UserSync:*` settings have been removed.

To replay: inspect the retained failure, correct availability/mapping/opt-in problems, and requeue the same job from Hangfire. Do not change the original message ID to bypass a conflict. Resolve conflicting IDs with the producer. Business failures receive protocol acceptance initially because interpretation is asynchronous; operators must monitor failed jobs. After successful-job expiry, obtain a source replay if needed.

`DeploymentScripts/TestExternalUserDeletion.ps1` now sends an XML fixture directly to KITOS using an installed client certificate. `-Duplicate` submits identical bytes twice. It checks durable acceptance only and never applies deletions; verify the pending item through the internal API and inspect failed jobs separately. The experimental PubSub callback, stub, receipt migrations, RabbitMQ transport and their test helpers have been removed from this branch. PubSub projects and shared Docker infrastructure match the branch base.

### Ordered cutover and rollback

These legacy-backlog steps apply only if an earlier experimental RabbitMQ version was deployed. Otherwise configure the direct callback and verify acceptance without a legacy migration.

1. Inventory deployed publishers, KITOS consumers, historical outbox/receipt state, all main/retry/dead queues and unacknowledged deliveries. Back up KITOS, Hangfire and broker state. This cannot be inferred from repository contents.
2. Pause the upstream user-change subscription. Keep the old KITOS consumer running until all accepted legacy deliveries are durably ingested. Account for pending receipt reservations, retry queues and dead letters; reconcile each source ID against KITOS. Do not purge queues or alter PubSub configuration/infrastructure as part of this implementation.
3. Resolve or preserve every outstanding message with a documented replay owner. If reconciliation cannot complete, postpone replacement; the new binary cannot drain legacy queues. Preserve the old executable/configuration for rollback.
4. Stop the old KITOS instance after reconciliation, deploy this version, configure certificates and mappings, enable the callback and validate worker health. Register the new callback URL with Beskedfordeler and resume the source subscription. Confirm a real event produces exactly one pending item and no access change.
5. For rollback, pause source delivery first. Drain or retain every accepted Hangfire job with its original payload; never discard Hangfire storage. Restore the old KITOS deployment and callback subscription only after reconciling accepted IDs and establishing how retained new jobs will be processed. Existing `ExternalUserChanges` provide idempotency across overlap. Do not claim a rollback complete while accepted jobs are stranded.

### Supported source interpretation

Only immediate `BrugerNedlagt` is supported. Subscribe to ORG_Bruger version 1.0 and the deactivation action UUID below, restricted to configured municipalities and one object registration. Planned deactivation is not supported; it needs effective-date scheduling and cancellation handling. Producer `Slet` and `Passiver` operations emit no events, so this is not comprehensive deletion reconciliation. Unit removal is not user deactivation.
| Source field under `Beskedkuvert/Filtreringsdata` | Mapping / validation |
| --- | --- |
| `Beskedtype/UUIDIdentifikator` | Must be `94ef3c82-10e9-4d12-a9d9-7a7242d5ba83` (`ORG_Bruger`). |
| `ObjektRegistrering/ObjektType/UUIDIdentifikator` | Must be `2b808769-8d09-4aa3-ac28-f8ac3526010f` (Bruger). |
| `ObjektRegistrering/ObjektHandling/UUIDIdentifikator` | Must be `9edb0c34-36e2-4d87-aba8-3ca7ade718ee` (`BrugerNedlagt`). |
| `ObjektRegistrering/ObjektId/UUIDIdentifikator` | Non-empty FK Bruger UUID becomes `ExternalUserUuid`. |
| `ObjektRegistrering/ObjektAnsvarligMyndighed/URNIdentifikator` | Responsible municipality CVR resolves through `Beskedfordeler:Organizations` to `OrganizationUuid`. |
| `TilladtModtager/URNIdentifikator` | The responsible municipality must appear in the delivered recipient list. Accept bare CVR, `urn:oio:cvr:` or `urn:oio:cvr-nr:`; never accept `*` as authorization. |
| `ObjektRegistrering/Registreringstidspunkt/TidsstempelDatoTid` | Required registration timestamp with explicit timezone, converted to UTC `OccurredAt`. This is registration time, not a future effective date. |

The existing SSO flow establishes the user-identity convention: `PrivilegeVerifiedState` passes the SAML UUID to `StsBrugerInfoService`, which reads FK Bruger by that UUID; `UserIdentifiedState` persists it as `SsoUserIdentity.ExternalUuid`. Confirm this with a real test user's event before activation. Never use a Person UUID, an actor UUID, or a CVR as the affected user's identity.

`Beskeddata` contains Base64 audit metadata (`Transport`, `Operation`, `TransactionID`, `Bruger`). Its `Bruger` is the actor making the change, not the affected user. The mapper does not decode or use it to decide who loses access. Exactly one `ObjektRegistrering` is supported per message; ambiguous multi-registration messages are rejected rather than partially acknowledged.

The agreed behavior is to translate immediate deactivation into a pending `Deleted` item for administrator review, preserving the existing apply/dismiss flow. No automatic access removal occurs at receipt.

## Validation and remaining acceptance checks

2026-10-13 local validation: both solution builds passed with zero warnings; the full unit suite passed, including 12 focused callback tests. Eight PostgreSQL persistence tests passed, including competing ingestion and replay after apply/dismiss. The PostgreSQL-backed Hangfire restart/failure/requeue test also passed. These storage tests exercise recovery and ingestion separately; they do not constitute an IIS-to-Beskedfordeler end-to-end acceptance test.

Unit tests cover certificate gating, durable enqueue failure/success, exact original payload retention, safe XML validation, replay and conflict behavior. Existing ingestion/review suites cover organization opt-in, identity matching, canonical timestamps and administrator resolution. `BeskedfordelerHangfireRecoveryTest` exercises PostgreSQL-backed Hangfire enqueue, database restart, retained failure and operator requeue; it requires Docker/Podman. `UserSyncPersistenceTest` exercises competing ingestion scopes and preserved apply/dismiss state against PostgreSQL. The target deployment boundary still requires integration acceptance.

Before production activation, demonstrate unauthorized, expired, revoked and rotated certificates through IIS; supported callback responses; duplicates and lost acknowledgements; concurrent workers; database outage and worker recycle; retry exhaustion and operator replay; retention cleanup leaving interpreted events intact; and lossless backlog reconciliation/rollback. Keep the callback disabled until these environment checks pass. Local quality-gate results are reported separately from deployment acceptance.

## References

- KOMBIT, _SF1500 Organisation_, integration version 3.3; nested `SF1500 Bilag v3.2/Beskeder/Beskedtype_ORG_Bruger_1.0.pdf`, sections 1-5 (the supplied 3.3 package retains a v3.2 directory name).
- KOMBIT, _SF1461 Modtag beskeder via Beskedfordeler_, integration version 2.0.
- KOMBIT, _D.09.02 Beskedfordeler-Besked-FåTilsendt-Snitflade_ (push interface).
- KOMBIT, _D.09.03 Beskedfordeler-Besked-Hent-Snitflade_ (pull alternative, not selected).
- KOMBIT, _B.08.09 Underbilag 2O - Beskedkuvert_ and `Beskedkuvert.xsd`.
- `AnvenderSystem.wadl` from SF1461 annexes confirms POST, application/xml and HTTP 200.
- `BeskeddataBase64ver.2.00.xsd`, `Beskedfordeler.wsdl`, and related operation/value-list schemas supplied with the integration documentation.
- Main implementation: `Presentation.Web/Infrastructure/Beskedfordeler/`, `Core.ApplicationServices/Users/ExternalUserChange*`, and `Core.DomainModel/Users/ExternalUserChange.cs`.
