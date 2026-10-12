# FK Organization user integration

## Status and scope

**Architecture change (2026-10-12):** RabbitMQ now owns delivery and retries. A persistent receipt ledger provides immediate duplicate/conflict checks. Existing outbox rows require the offline cutover below before enabling the new consumer. Production broker, certificate and KOMBIT acceptance checks remain deployment prerequisites.

This document describes the FK Organization user-change based on the connection to KOMBIT's Beskedfordeler. The KITOS and PubSub user-change pipeline is implemented. A disabled-by-default Beskedfordeler callback and the SF1500 `ORG_Bruger` immediate-deactivation mapper are implemented with the complete SF1461 callback schema dependencies. Production activation still requires verified municipality mappings, certificate/subscription provisioning, and external-test acceptance. The current PubSub JSON stub is a development/test tool, not a production Beskedfordeler callback.

The initial supported source event is `BrugerNedlagt` (user becomes inactive), represented by the existing `Deleted` review item. This is not comprehensive detection of physical deletions: the producer explicitly emits no events for `Slet` or `Passiver`. KITOS records that report for review by the affected organization's local administrator rather than immediately removing access. The flow is designed to accept events from an upstream integration, durably hand them to KITOS, and apply or dismiss them through the internal API.

## End-to-end flow

```text
External source
    -> PubSub receipt reservation + confirmed RabbitMQ publish
    -> dedicated RabbitMQ consumer
    -> HTTPS KITOS integration API
    -> pending ExternalUserChange
    -> organization's local administrator reviews it
    -> apply deletion or dismiss
```

The normalized event is deliberately independent of any particular source transport. KOMBIT XML must be translated to this contract by a producer-specific mapper in the Beskedfordeler adapter; the KITOS ingestion API does not accept KOMBIT XML.

### 1. PubSub event, receipt ledger and RabbitMQ

`PubSub.Core.DomainModel.UserSync.UserDeletionEvent` is the normalized event:

| Field               | Meaning                                                                |
| ------------------- | ---------------------------------------------------------------------- |
| `ExternalMessageId` | Stable source-message identity, unique in the PubSub receipt ledger and KITOS. |
| `OrganizationUuid`  | KITOS UUID of the organization whose access is affected.               |
| `ExternalUserUuid`  | External/SAML identity UUID used to find the KITOS user.               |
| `ChangeType`        | `1` (`Deleted`) is currently the only supported value.                 |
| `OccurredAt`        | Optional UTC event time.                                               |

`UserChangeIngress` validates and normalizes the event to UTC microseconds, then reserves its message ID in `UserChangeReceipts`. The ledger stores only a versioned SHA-256 hash of the canonical organization UUID, external user UUID, change type and optional occurrence time, plus reservation/acceptance timestamps. A null acceptance timestamp means Pending; a populated timestamp means Accepted. The primary key enforces unique IDs across instances. Different hashes conflict immediately; equivalent accepted replays succeed without another publish. Receipts have no implicit expiry and contain no delivery payload or retry schedule.

For a pending receipt, `UserChangePublisher` publishes the normalized `{ "Payload": ... }` envelope persistently and with mandatory routing, contract version, message ID and correlation ID. RabbitMQ.Client 7.1.2 publisher confirmation tracking rejects nacks and unroutable returns. A 30-second timeout bounds connection, topology declaration and confirmation. Only after confirmation does PubSub commit receipt acceptance and return success. No database transaction spans a broker call. Timeout, cancellation or connection loss may mean the broker accepted a message: the receipt remains pending and equivalent source/operator replay republishes safely. A pending receipt cannot deliver on its own.

`UserChangeConsumer` runs only when `UserSync:Enabled` is true. It uses a dedicated channel with manual acknowledgements, prefetch 1 and sequential dispatch, so its KITOS token cache is not accessed concurrently. It validates the queued contract, sends HTTPS JSON with a normal KITOS PubSub account token, and refreshes/retries once on 401. Login and sends are bounded by a 90-second attempt deadline (individual HTTP calls retain the 30-second client timeout). It acknowledges only after a 2xx KITOS response or a confirmed retry/dead-letter transfer. Network errors, timeouts, 5xx, 408, 429, 401 and 403 retry indefinitely with delays of 2, 4, 8, 16, 32, 64, 128 and then 256 seconds. Other 4xx, including 409, and invalid queue contracts go to the durable dead-letter queue. Failed transfers leave the original unacknowledged, close the session and back off five seconds before reconnecting and subscribing again. A crash between transfer and ack can duplicate work; KITOS ingestion remains idempotent.

The dedicated topology is a durable quorum main queue, eight quorum retry queues and a quorum dead-letter queue. Retry queues use TTL with `at-least-once` dead-lettering and `reject-publish` overflow to return safely to main. All queues explicitly disable broker delivery limits (`x-delivery-limit=-1`); main/dead queues have no expiry. Generic topic consumers are unchanged. Require RabbitMQ 4.1 or later with quorum support; do not apply policies introducing message expiry, drop-head overflow or delivery limits. Local Compose pins the 4.1 series and retains the existing data volume; production must verify supported patch version, persistent disks and quorum replication separately. Do not upgrade an existing broker data directory without following RabbitMQ's supported upgrade path.

The `UserSyncStubController` (`POST /user-sync/stub`) only exists to exercise this pipeline. It is available only in Development, Local, or Test, requires `UserSync:EnableStub` and `UserSync:Enabled`, and is hidden in a Production deployment. It uses the PubSub publish policy. `DeploymentScripts/TestUserSync.ps1` is a test helper; it requires an HTTPS base URL and reads the publish token from `USER_SYNC_PUBLISH_TOKEN`.

### 2. KITOS ingestion

PubSub posts to `POST /api/v2/integrations/fk-organisation/user-changes` with the normalized envelope:

```json
{
  "payload": {
    "externalMessageId": "source-message-id",
    "organizationUuid": "00000000-0000-0000-0000-000000000001",
    "externalUserUuid": "00000000-0000-0000-0000-000000000002",
    "changeType": 1,
    "occurredAt": "2026-10-05T08:00:00Z"
  }
}
```

The route is token-authenticated and additionally requires the current account to be a usable KITOS API user marked as a PubSub user (`HasApiAccess` and `IsPubSubUser`). This database permission check means changing or revoking the account permission applies even to an already-issued token. Global administrators manage the marker through `GET /api/v2/internal/users/pubsub-users` and `PATCH /api/v2/internal/users/pubsub-users/{userUuid}?requestedValue=true|false`.

The controller rejects malformed JSON, missing payloads, and oversized bodies. The application service accepts only a non-empty message ID of at most 200 characters, known organization and non-empty external-user UUIDs, the `Deleted` change type, and an optional UTC occurrence timestamp. Invalid input returns a client error; an unknown organization returns not found.

Events are accepted only for organizations whose FK Organisation **users connection** is enabled (see below). For any other organization the endpoint returns `409 Conflict` before deduplication or persistence. 409 was chosen over 422 because the request is well-formed but conflicts with the organization's current state, it maps to the existing `OperationFailure.Conflict`, and it is a permanent client error that `UserChangeConsumer` dead-letters instead of retrying (unlike 401/403/408/429). Identical replays for connected organizations remain idempotent.

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

## Persistence and deployment

KITOS adds the `ExternalUserChanges` table with a unique source-message ID, organization/status/receipt-time index, references to the organization, matched user, and resolving user, and a concurrency version. The `User` table gains the `IsPubSubUser` flag (default false). The `Organization` table gains `FkOrgUsersConnected` (default false), `FkOrgUsersConnectedAt` and `FkOrgUsersConnectedByUserId` (FK to `User`, set null on delete). These changes are in the KITOS EF Core migrations `AddExternalUserChanges`, `AddPubSubUser` and `AddFkOrgUsersConnection`. Existing organizations start disconnected, so a local administrator must enable the users connection before PubSub deliveries are accepted.

PubSub migration `AddUserChangeReceipts` adds the receipt ledger and a `RabbitMqMigratedAt` checkpoint to the retained historical `UserChangeDeliveries` table. Apply this migration as well as the KITOS migrations. The old table is used only for offline cutover and startup reconciliation; no delivery worker polls it. Keep PubSub's KITOS account credentials in deployment-managed secret configuration; do not commit credentials or certificate private keys.

Relevant PubSub configuration keys are:

- `UserSync:Enabled` — run the RabbitMQ consumer and allow ingress.
- `UserSync:KitosEndpoint` — HTTPS KITOS ingestion URL.
- `UserSync:KitosEmail` and `UserSync:KitosPassword` — KITOS PubSub account credentials.
- `UserSync:EnableStub` — explicitly enable the test-only JSON publisher where allowed. Its response contains `externalMessageId` and `queueAccepted`.
- `UserSync:QueuePrefix` — defaults to `kitos.user-sync.v1`; keep stable after publishing. Main uses the prefix, retries append `.retry.1` through `.retry.8`, and dead letters append `.dead`.
- Existing `RabbitMQ:HostName`, `RABBIT_MQ_USER`, `RABBIT_MQ_PASSWORD` select the broker; optional `RabbitMQ:Port`, `VirtualHost`, `TlsEnabled` and `TlsServerName` configure the connection. TLS uses normal certificate/hostname validation; enable it for production broker transport.

The PubSub consumer will not start delivery if disabled and fails startup validation if enabled without an HTTPS endpoint or the required account credentials. The Beskedfordeler callback has separate opt-in configuration described below; actual environment certificates, trust installation, and callback provisioning remain deployment work.

### Cutover, replay and rollback

1. Stop ingress at the upstream/proxy and stop **all** old PubSub hosts/workers. Back up PubSub and KITOS databases and broker data. Keep source mailbox messages available for replay. Apply `AddUserChangeReceipts`; keep all historical migrations and old rows.
2. With ingress paused, run `dotnet PubSub.Application.Api.dll --migrate-user-sync-outbox` from the deployed application directory with its normal database/broker configuration. This offline mode never starts HTTP endpoints or hosted consumers. It checks every uncheckpointed legacy row in bounded batches, validates the old payload and identity, reserves the receipt, publishes pending rows to main and dead-lettered rows to `.dead` with confirmation, then atomically commits receipt acceptance and the row checkpoint. Delivered rows only seed receipts. Empty outboxes are explicitly checked and report zero transfers.
3. Rerun the same command after interruption. A confirmation followed by a failed checkpoint can produce duplicates, never silent loss. The command fails on corrupt/conflicting legacy data; investigate rather than deleting it. Verify every legacy row has `RabbitMqMigratedAt`, receipt hashes match, and main/dead queue counts account for duplicates and any authorized replay. Enabled normal startup refuses unreconciled legacy rows.
4. Resume only the new host/consumer and ingress. Keep old rows and backups for audit. Monitor pending receipts and mailbox status; XML status 53 can stop the mailbox, so do not assume automatic source retry. Recover the dependency, coordinate mailbox restart with the provider and replay the original source message ID and event.

For pending receipts, replay the original source XML through the authenticated callback. Do not mark receipts accepted by hand: pending receipts contain no payload. For dead letters, an operator must inspect the failure, fix the cause, and republish the **same normalized body and message ID**, with `Type=kitos.user-deletion.v1`, persistent delivery and mandatory publisher confirms. Use a controlled consumer/replay client: acknowledge the dead-letter source only after confirmed routing to main. Do not send accepted dead letters back through ingress: accepted receipts intentionally short-circuit publishing. Keep production stub disabled.

For rollback, pause ingress and stop the new consumer first. Inventory main, retry (including expired retained messages) and dead queues and reconcile every message with KITOS and legacy rows. New messages have no legacy payload row; export/reconstitute them through a reviewed offline procedure before starting an old worker. Already delivered events remain safe to replay with the original identity. Never simply start both workers or clear queues/checkpoints. Preserve the receipt ledger and historical migration; destructive downgrade/table removal is deferred until reconciliation and recovery are verified.

Container validation uses isolated RabbitMQ 4.1 and PostgreSQL 16 instances. The 15 broker/database cases passed, including the existing KITOS persistence suite. The full `pwsh ./scripts/quality-check.ps1 -Fix` gate passed with zero build warnings for both solutions and all unit suites (including 107 PubSub tests); the code-quality self-review is complete. The consumer recovery scenario uses an idempotent HTTP test handler; real KITOS identity/review persistence is covered separately by the PostgreSQL suite. Live KOMBIT/mTLS acceptance and production cutover/rollback rehearsal remain unchecked below.

### Operational checks

`/health/user-sync` runs bounded database and passive queue checks and returns aggregate health (no event data). The registered `UserChangeHealthCheck` also provides pending receipt count/oldest age, main and retry depths, consumer count and dead-letter count to health-check integrations. Alert on degraded/unhealthy status, stalled pending receipts and dead letters. Broker management/Prometheus should monitor persistent storage, quorum membership, ready/unacked queue depth and oldest-message age. The `Kitos.PubSub.UserSync` .NET meter exposes confirmed/failed publishes, retries, dead letters, accepted deliveries, connected consumer sessions and delivery age since the latest transfer. Configure a metrics collector; declaring a meter alone does not export it. Logs contain message IDs and failure categories, never bodies or XML diagnostics.

## KOMBIT Beskedfordeler connection

### Implemented integration

The target transport is Beskedfordeler's synchronous REST push callback over mutual TLS. The generic Beskedkuvert envelope uses `ModtagBeskedInput` and contains a `Haendelsesbesked`; its extensible `Beskeddata` element does not define the payload schema for a specific producer event. The Beskedfordeler integration is therefore a separate inbound adapter in PubSub, not a change to the normalized PubSub-to-KITOS contract:

1. Beskedfordeler calls a dedicated HTTPS callback using its XML `ModtagBeskedInput`.
2. PubSub validates the envelope and authenticates the client certificate against the configured environment-specific KOMBIT/Beskedfordeler trust.
3. The adapter maps the confirmed immediate-deactivation envelope to `UserDeletionEvent` and reserves its receipt and publishes it to RabbitMQ with confirmation.
4. Only after routed broker confirmation and receipt acceptance does PubSub acknowledge with the documented XML `ModtagBeskedOutput` success (`StandardRetur.StatusKode` 20 and HTTP 200).
5. The dedicated RabbitMQ consumer forwards the normalized JSON event to KITOS; KITOS records it for local-administrator review.

The callback must use server-managed TLS certificate material, require and validate client certificates, separate test and production trust, and allow controlled certificate rotation. It must not use the KITOS PubSub bearer token to authenticate Beskedfordeler. The bearer token remains for the outbound PubSub-to-KITOS hop only. The source `BeskedID` should be the idempotency key: equivalent replays can be acknowledged without creating another delivery, while reuse of an ID for a different event must not be acknowledged as successful. Confirm-and-persist-before-ack keeps the handoff durable; an acknowledgment means PubSub accepted responsibility, not that KITOS has finished delivering or an administrator has applied the change.

### Prepared receiver and configuration

`POST /user-sync/beskedfordeler` is a separate PubSub callback. It is absent operationally (404) unless `Beskedfordeler:Enabled` is true. It accepts `application/xml` or `text/xml`, requires HTTPS and a client certificate, and does not accept a bearer token as callback authentication. Certificate authorization requires both an environment-specific SHA-256 certificate fingerprint and a valid OS-trusted chain with online revocation checking and client-authentication usage. Configure the complete trust chain on the server. No forwarded-certificate HTTP header is trusted.

When enabled, Kestrel requests certificates on HTTPS connections and permits TLS 1.2/1.3. Other PubSub endpoints retain their existing authentication; the callback itself rejects missing certificates. Configure server certificates through normal Kestrel deployment configuration. TLS termination at a proxy needs a separately reviewed trust/forwarding design; this implementation expects the client certificate on the application's TLS connection.

| Configuration | Requirement |
| --- | --- |
| `Beskedfordeler:Enabled` | Defaults to false when absent. Keep disabled until municipality mappings, provisioning and external-test acceptance are complete. |
| `Beskedfordeler:SchemaDirectory` | Optional override for the complete callback XSD directory. Defaults to `UserSync/Beskedfordeler/Schemas` under the application output directory; bundled schemas are copied to build/publish output and compiled at startup. |
| `Beskedfordeler:ClientCertificateSha256` | Array of 64-character hexadecimal SHA-256 certificate fingerprints for the selected environment. Multiple entries support overlap during rotation; restart after changes. |
| `Beskedfordeler:Organizations` | Required CVR-to-KITOS-organization UUID dictionary. Keys must be eight ASCII digits; values must be non-empty and unique. Use KITOS UUIDs, not FK Organisation UUIDs. Invalid or empty mappings fail startup. |
| `UserSync:Enabled` | Must also be true, with the KITOS endpoint and credentials configured. |

For environment variables, use `Beskedfordeler__Enabled`, `Beskedfordeler__SchemaDirectory`, and `Beskedfordeler__ClientCertificateSha256__0` (and subsequent indices). Use `Beskedfordeler__Organizations__12345678=<KITOS organization UUID>` for each subscribed municipality. Verify this association against KITOS organisation records; configuration does not discover or validate remote records. Restart after changes. Trust, mappings and fingerprints must be supplied separately for test and production; no certificates, fingerprints or live municipality mappings are bundled.

The reader caps XML at 1 MiB of characters and the endpoint caps the HTTP body at 1 MiB. It prohibits DTDs and external entity resolution and validates XML against the configured schema set. All XSDs are loaded explicitly from the configured local directory; XML cannot select or download schemas. Unknown or invalid structure is rejected. The matching SF1461 callback XSDs and all their transitive dependencies are bundled under `PubSub.Application.Api/UserSync/Beskedfordeler/Schemas/`. `wadl/AnvenderSystem.wadl` is an unchanged contract reference; its original `../xsd` path corresponds to the repository's `Schemas` directory. It is not used for runtime schema resolution.

The receiver uses `Haendelsesbesked/BeskedId/sd:UUIDIdentifikator`, canonicalized as a GUID, for the receipt identity and accepts envelope version `1.0`. `OrgBrugerDeletionMapper` implements `IBeskedfordelerDeletionMapper` using SF1500's `Beskedtype_ORG_Bruger_1.0.pdf` (2023-12-06), sections 1-3:

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

After a successful mapping, the receiver awaits routed publisher confirmation and receipt acceptance before returning `ModtagBeskedOutput` with `StandardRetur/StatusKode` 20. Equivalent normalized replays succeed; conflicting IDs return 40. XML validation failures and invalid normalized events return 40; unsupported source events, missing producer fields, unresolved CVRs, unauthorized recipient mappings and ambiguous registrations return 51, database and broker handoff failures return 53, and unsupported envelope versions return 55. Protocol replies use HTTP 200 and `application/xml`; authentication and disabled-route failures use HTTP 403/404. Unexpected failures/cancellation do not produce a success acknowledgment. Payloads and XML parser diagnostics are not logged.

D.09.02 sections 5.2 and 10.2 define protocol codes 20/40/51/53/55. Section 5.2 also contains an apparent typo referring to HTTP status 20; the supplied `AnvenderSystem.wadl` explicitly specifies HTTP 200, resolving that ambiguity. The success body still carries XML status 20. The document says 40 temporarily interrupts delivery and other protocol errors stop sending from the mailbox. Restrict the subscription to supported events and configured municipalities. An unexpected event is not acknowledged or discarded; it requires operator investigation because status 51 stops delivery from the mailbox.

### Subscription and remaining deployment work

Use the `ORG_Bruger` message type and version 1.0, filtered to `ObjektHandling` UUID `9edb0c34-36e2-4d87-aba8-3ca7ade718ee`. SF1500 section 4 specifies BeskedModtag role `Modtag`, the sending authority's CVR, message type `94ef3c82-10e9-4d12-a9d9-7a7242d5ba83`, sensitivity `1d81c472-0808-44cc-963d-f5ef0170ae1d`, and municipal administrative area `*`. Verify the configured subscription in the external test environment, including that every delivered message has exactly one relevant registration.

Do not subscribe this adapter to `BrugerPlanlagtNedlagt` (`b91ebc7e-7540-44a0-a360-ea58a3adc0e2`): planned changes emit a notification at registration time, with no second notification when they take effect. Supporting them requires separate effective-date lookup/scheduling and handling later changes or cancellation. Removing a user from one organisation unit is also not interpreted as deactivating the user. `Slet` and `Passiver` emit no events according to the producer contract; comprehensive deletion detection would require a separately designed reconciliation mechanism.

Before activation, configure and verify CVR-to-KITOS UUID mappings, approve service agreements, register the callback URL/server certificate, install environment-specific trust, configure fingerprints, and create the mailbox/subscription. Run external-test delivery with a real matching SSO identity, duplicate messages, failed persistence, unknown municipalities, and certificate rotation/revocation. Confirm failure recovery so a rejected message does not indefinitely block the mailbox. The development JSON stub must not be registered as the production callback.

Pull/AMQP, value-list SOAP operations, planned-event scheduling and deletion reconciliation are out of scope for this adapter. The callback remains disabled unless explicitly enabled.

## Verification coverage

The branch adds unit coverage for event ingestion, duplicate/conflict handling, pending-change reads, apply/dismiss and batch resolution, PubSub token handling, confirmation sequencing and manual-ack delivery behavior, integration endpoint authentication, plus receipt, cutover and real RabbitMQ/PostgreSQL coverage in `UserSyncRabbitMqTest`. Key suites are `Tests.Unit.Core.ApplicationServices`, `Tests.Unit.Presentation.Web`, `PubSub.Test`, and `Tests.Container`. `BeskedfordelerTest` covers response namespaces/codes, duplicate/conflicting IDs, failure to persist, unsupported mapping/version, disabled/unauthenticated callbacks, untrusted certificates, and bounded schema-validating XML parsing with DTD rejection. `OrgBrugerDeletionMapperTest` additionally compiles the bundled official schemas, validates a synthetic source message and success response, exercises durable mapping/replay, and rejects unsupported, ambiguous and unresolved events. Synthetic fixtures are derived from the contract, not captured provider traffic. Actual mTLS handshake tests, certificate rotation/revocation acceptance tests and end-to-end Beskedfordeler interoperability remain required before activation.

## Plan: RabbitMQ delivery for FK Organisation user changes

### Problem / Goal

Replace the database delivery outbox with `KOMBIT -> PubSub REST endpoint -> RabbitMQ -> dedicated consumer -> KITOS`. Preserve the existing mTLS/XML callback, supported event mapping, KITOS JSON contract, and administrator review workflow. The implementation is described above; unchecked verification items below remain explicit deployment or acceptance work.

The user explicitly requires immediate rejection of conflicting message IDs at the callback. A small persistent receipt store therefore remains in PubSub, but RabbitMQ owns queued payloads and delivery retries. Delivery is at least once; KITOS remains responsible for preventing duplicate business changes.

### 1. Gather information

- [x] Inspect `BeskedfordelerController`, `BeskedfordelerReceiver`, `OrgBrugerDeletionMapper`, `UserChangeOutbox`, `UserChangeDeliveryWorker`, `KitosTokenProvider`, and KITOS ingestion/idempotency handling.
- [x] Inspect the existing RabbitMQ publisher, consumer, connection manager, Docker configuration, migrations, and unit/container tests. The generic consumer uses `autoAck: true`; its publisher does not explicitly enable publisher confirms. Neither can be reused unchanged for this delivery guarantee.
- [x] Confirm the target transport with the user: REST push into PubSub, RabbitMQ handoff, consumer delivery to KITOS. Beskedfordeler AMQP pull is outside this change.
- [x] Resolve duplicate/conflict semantics with the user: retain synchronous conflict rejection using a separate persistent receipt store.
- [ ] Before implementation, review this plan and confirm acceptance criteria below. Verify production broker version, persistence/replication, TLS ingress, and whether any environment contains old outbox rows. These are deployment checks, not assumptions that existing data can be discarded.

Proposed implementation boundaries: use a dedicated user-sync queue and consumer inside the existing PubSub host, reusing broker infrastructure and the KITOS token provider where appropriate. Keep generic topic/subscriber delivery behavior outside the scope of this change. Validate/map XML at ingress and queue the normalized event; the consumer validates the queued contract and calls KITOS.

### 2. Create tests and necessary validation

- [x] Extend callback tests: invalid authentication/XML/mapping never publishes; new messages cannot receive XML status 20 until a routed publish is confirmed and the receipt is marked accepted; database failure, broker failure, nack, unroutable return, timeout, and cancellation never produce success.
- [x] Test receipts using a relational database: unique message ID, deterministic versioned hash of normalized event fields, equivalent replay, conflicting replay, and concurrent equal/conflicting requests across instances. Hash canonical values, not raw XML or incidental JSON formatting; align timestamp normalization with KITOS.
- [x] Inject crashes after receipt reservation, after broker confirmation, and after receipt acceptance but before the HTTP response. Equal retries must recover pending receipts, accepted retries must succeed, conflicts must remain rejected, and duplicate queue deliveries must not create duplicate KITOS changes.
- [x] Test the dedicated consumer: manual ack after KITOS success; one token refresh/retry on 401; bounded HTTP attempts; delayed retry for network errors, timeouts, 5xx, 408, 429, 401, and 403; permanent rejection including 409 routed to a dead-letter queue. Preserve current error classification unless contract tests identify a necessary change.
- [x] Use real RabbitMQ container tests for publisher confirms/returns, broker restart with persistent storage, unacked redelivery after consumer termination, reconnect/re-subscription, delayed retry, and failure of retry/dead-letter transfers. Mock-only tests cannot prove broker durability.
- [x] Test migration with empty and populated outboxes, including delivered, pending, and dead-lettered rows, plus interruption/restart of migration. Retain existing KITOS idempotency, authorization, and review tests.

### 3. Implement

- [x] Add a receipt entity/repository and EF migration with unique `ExternalMessageId`, versioned normalized-event hash, state (`Pending`/`Accepted`), and timestamps. No delivery payload, attempt schedule, or background delivery polling belongs in this store. Keep receipts until an explicit retention policy is agreed; do not expire them implicitly.
- [x] Implement receipt/publish sequencing: authenticate, validate and map; atomically reserve or compare the receipt; reject differing hashes with existing conflict status 40; return success for an already accepted equivalent receipt; otherwise publish persistently with mandatory routing and publisher confirmations, then mark accepted and return status 20. Never hold a database transaction open across a broker call.
- [x] Treat a publish timeout or connection loss as an ambiguous result. Keep the receipt pending and do not acknowledge success. On equivalent KOMBIT redelivery, republish and retry acceptance. A crash after confirmation can produce duplicate messages; never pretend the database and broker commit atomically. A pending receipt cannot deliver anything on its own: recovery depends on source redelivery or operator replay.
- [x] Add a dedicated durable main queue, delayed-retry topology, and durable dead-letter queue with explicit declarations, persistent messages, message ID, contract version, and correlation metadata. Reuse the configured broker with separate queue names. Verify RabbitMQ.Client 7.1.2 confirmation/return APIs and supported broker topology before coding.
- [x] Replace `UserChangeDeliveryWorker` database polling with a hosted consumer using manual acknowledgements, bounded prefetch/concurrency, cancellation-aware shutdown, and connection/channel recovery. Extract/reuse HTTPS KITOS delivery and token renewal; check token-cache concurrency if parallel delivery is enabled.
- [x] Acknowledge a main-queue delivery only after KITOS accepts it or after a confirmed, routed transfer to the retry/dead-letter queue. On failed transfer leave/requeue the original safely and back off instead of spinning. A crash between transfer confirmation and source ack may duplicate a message and must be tested.
- [x] Preserve delayed transient retries with a capped backoff (current maximum 256 seconds) and retain permanent failures in the dead-letter queue for explicit operator replay. Do not silently discard messages after an attempt threshold. Use broker-supported at-least-once dead-lettering for delayed return to the main queue, or another confirmed transfer mechanism; ordinary TTL/dead-letter defaults must not introduce an unconfirmed loss window. Verify delivery limits, expiry, and overflow policies do not discard messages.
- [x] Update `BeskedfordelerReceiver`, controller error handling, DI, and the development stub to use the new receipt/publisher boundary. The stub reports message ID and queue acceptance, not removed outbox delivery fields. Keep mTLS, XML schema checks, supported event scope, feature flags, and KITOS business behavior.
- [x] Add operational checks and metrics for broker connectivity, pending receipts, confirm failures, queue age/depth, retries, consumer health, and dead letters. Log identifiers and failure categories without event payloads. Document replay preserving the original message ID and event, plus recovery of stalled KOMBIT mailboxes: current documentation says protocol status 53 can stop mailbox delivery, so automatic retries must not be assumed.
- [x] Implement a staged cutover: pause ingress and the old worker; seed receipts from existing outbox records; migrate pending payloads with publisher confirms and dead-lettered payloads to the dead-letter queue; preserve delivered receipts without redelivery. Make migration restartable and reconcile every record. If no outbox data exists, verify that explicitly and skip transfer. Resume only the new path after reconciliation.
- [x] Remove runtime outbox dependencies and polling after cutover. Preserve historical migrations and existing data for rollback/audit; defer destructive table removal until migrated counts and recovery have been verified. Rollback must reconcile queued work before restarting the old path. Update this document's current-behavior sections, deployment configuration, and test helpers when implementation is complete.

### 4. Validate

- [x] Run `dotnet test PubSub.Test`, affected KITOS ingestion tests, and the relevant `Tests.Container` broker/database suites. Run `pwsh ./scripts/quality-check.ps1` and complete the repository's code-quality self-review for implementation changes.
- [ ] Demonstrate the complete flow with KITOS unavailable: callback accepts only confirmed queue deliveries, messages survive broker/consumer restart, and recovery creates one KITOS review item per event.
- [ ] Demonstrate receipt database unavailability, conflicting IDs, ambiguous publish outcomes, consumer termination after KITOS commit but before ack, and retry/dead-letter target failure. Verify recovery without silent loss or duplicate business effects.
- [ ] Verify broker durability/replication and retention settings in the actual deployment, plus real mTLS handshakes, certificates, and KOMBIT external-test success/failure responses. Verify mailbox recovery after callback failure before production activation.
- [ ] Rehearse cutover and rollback with representative existing rows; verify no pending/dead-lettered payload is stranded and no active delivery worker queries the old outbox.
- [ ] Confirm acceptance criteria: the requested RabbitMQ flow is active; conflicting IDs are rejected at the callback; durable broker acceptance precedes callback success; KITOS acceptance or confirmed transfer precedes consumer ack; retries/dead letters are recoverable; source and broker redelivery are idempotent; receipt storage contains no delivery queue; existing KITOS authorization and review behavior are preserved.

## References

- KOMBIT, _SF1500 Organisation_, integration version 3.3; nested `SF1500 Bilag v3.2/Beskeder/Beskedtype_ORG_Bruger_1.0.pdf`, sections 1-5 (the supplied 3.3 package retains a v3.2 directory name).
- KOMBIT, _SF1461 Modtag beskeder via Beskedfordeler_, integration version 2.0.
- KOMBIT, _D.09.02 Beskedfordeler-Besked-FåTilsendt-Snitflade_ (push interface).
- KOMBIT, _D.09.03 Beskedfordeler-Besked-Hent-Snitflade_ (pull alternative, not selected).
- KOMBIT, _B.08.09 Underbilag 2O - Beskedkuvert_ and `Beskedkuvert.xsd`.
- `AnvenderSystem.wadl` from SF1461 annexes confirms POST, application/xml and HTTP 200.
- `BeskeddataBase64ver.2.00.xsd`, `Beskedfordeler.wsdl`, and related operation/value-list schemas supplied with the integration documentation.
- Main implementation: `PubSub.Application.Api/UserSync/`, `PubSub.Core.DomainModel/UserSync/`, `Core.ApplicationServices/Users/ExternalUserChange*`, `Core.DomainModel/Users/ExternalUserChange.cs`, and `Presentation.Web/Controllers/API/V2/Integration/ExternalUserChangeIngestionController.cs`.

RabbitMQ implementation references: [Client 7.1.2 confirmation tracking](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/v7.1.2/projects/RabbitMQ.Client/CreateChannelOptions.cs), [quorum at-least-once dead-lettering](https://www.rabbitmq.com/docs/quorum-queues#dead-lettering).
