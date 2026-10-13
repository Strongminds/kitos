# FK Organization user integration

## Status and scope

**Architecture revision (2026-10-13):** KITOS now owns RabbitMQ consumption and invokes ingestion directly in a fresh dependency scope. The ingestion HTTP endpoint, delivery credentials and IsPubSubUser marker have been removed. IIS lifecycle, production connectivity and cutover rehearsal remain deployment checks.

**Architecture change (2026-10-12):** RabbitMQ now owns delivery and retries. A persistent receipt ledger provides immediate duplicate/conflict checks. Existing outbox rows require the offline cutover below before enabling the new consumer. Production broker, certificate and KOMBIT acceptance checks remain deployment prerequisites.

This document describes the FK Organization user-change based on the connection to KOMBIT's Beskedfordeler. The KITOS and PubSub user-change pipeline is implemented. A disabled-by-default Beskedfordeler callback and the SF1500 `ORG_Bruger` immediate-deactivation mapper are implemented with the complete SF1461 callback schema dependencies. Production activation still requires verified municipality mappings, certificate/subscription provisioning, and external-test acceptance. The current PubSub JSON stub is a development/test tool, not a production Beskedfordeler callback.

The initial supported source event is `BrugerNedlagt` (user becomes inactive), represented by the existing `Deleted` review item. This is not comprehensive detection of physical deletions: the producer explicitly emits no events for `Slet` or `Passiver`. KITOS records that report for review by the affected organization's local administrator rather than immediately removing access. The flow is designed to accept events from an upstream integration, durably hand them to KITOS, and apply or dismiss them through the internal API.

## End-to-end flow

```text
External source
    -> PubSub receipt reservation + confirmed RabbitMQ publish
    -> KITOS-hosted RabbitMQ consumer
    -> scoped ExternalUserChangeIngestionService
    -> pending ExternalUserChange
    -> organization's local administrator reviews it
    -> apply deletion or dismiss
```

The normalized event is deliberately independent of any particular source transport. KOMBIT XML must be translated to this contract by a producer-specific mapper in the Beskedfordeler adapter; the KITOS consumer accepts only the normalized queue contract.

### 1. PubSub event, receipt ledger and RabbitMQ

`UserSync.Transport.UserDeletionEvent` is the normalized event:

| Field               | Meaning                                                                |
| ------------------- | ---------------------------------------------------------------------- |
| `ExternalMessageId` | Stable source-message identity, unique in the PubSub receipt ledger and KITOS. |
| `OrganizationUuid`  | KITOS UUID of the organization whose access is affected.               |
| `ExternalUserUuid`  | External/SAML identity UUID used to find the KITOS user.               |
| `ChangeType`        | `1` (`Deleted`) is currently the only supported value.                 |
| `OccurredAt`        | Optional UTC event time.                                               |

`UserChangeIngress` validates and normalizes the event to UTC microseconds, then reserves its message ID in `UserChangeReceipts`. The ledger stores only a versioned SHA-256 hash of the canonical organization UUID, external user UUID, change type and optional occurrence time, plus reservation/acceptance timestamps. A null acceptance timestamp means Pending; a populated timestamp means Accepted. The primary key enforces unique IDs across instances. Different hashes conflict immediately; equivalent accepted replays succeed without another publish. Receipts have no implicit expiry and contain no delivery payload or retry schedule.

For a pending receipt, `UserChangePublisher` publishes the normalized `{ "Payload": ... }` envelope persistently and with mandatory routing, contract version, message ID and correlation ID. RabbitMQ.Client 7.1.2 publisher confirmation tracking rejects nacks and unroutable returns. A 30-second timeout bounds connection, topology declaration and confirmation. Only after confirmation does PubSub commit receipt acceptance and return success. No database transaction spans a broker call. Timeout, cancellation or connection loss may mean the broker accepted a message: the receipt remains pending and equivalent source/operator replay republishes safely. A pending receipt cannot deliver on its own.

`UserChangeConsumer` is registered only by KITOS when `UserSync:ConsumerEnabled` is true. It uses a dedicated connection/channel, manual acknowledgements, prefetch 1 and sequential dispatch. It validates the existing v1 envelope, message ID and contract type, then resolves `ExternalUserChangeIngestionService` in a fresh async scope without an HTTP request or authenticated user. Success follows database commit, including committed duplicate detection. Invalid contracts, bad input, unknown organizations, disabled users connections and conflicting IDs go to the durable dead-letter queue. Infrastructure failures retry indefinitely after 2, 4, 8, 16, 32, 64, 128 and then 256 seconds. Acknowledgement follows durable ingestion or confirmed routed transfer only. Failed transfers leave the original unacknowledged, close the session and back off five seconds before reconnecting. Cancellation closes the session and allows redelivery. A crash after commit or transfer but before acknowledgement can duplicate delivery; ingestion remains idempotent. The reusable contract, publisher, topology and consumer engine live in `UserSync.Transport`, which has no dependency on either API host.

The dedicated topology is a durable quorum main queue, eight quorum retry queues and a quorum dead-letter queue. Retry queues use TTL with `at-least-once` dead-lettering and `reject-publish` overflow to return safely to main. All queues explicitly disable broker delivery limits (`x-delivery-limit=-1`); main/dead queues have no expiry. Generic topic consumers are unchanged. Require RabbitMQ 4.1 or later with quorum support; do not apply policies introducing message expiry, drop-head overflow or delivery limits. Local Compose pins the 4.1 series and retains the existing data volume; production must verify supported patch version, persistent disks and quorum replication separately. Do not upgrade an existing broker data directory without following RabbitMQ's supported upgrade path.

The `UserSyncStubController` (`POST /user-sync/stub`) only exists to exercise this pipeline. It is available only in Development, Local, or Test, requires `UserSync:EnableStub` and `UserSync:Enabled`, and is hidden in a Production deployment. It uses the PubSub publish policy. `DeploymentScripts/TestUserSync.ps1` is a test helper; it requires an HTTPS base URL and reads the publish token from `USER_SYNC_PUBLISH_TOKEN`.

### 2. KITOS ingestion

KITOS consumes the unchanged `{ "Payload": ... }` envelope with `Type=kitos.user-deletion.v1` and a matching AMQP `MessageId`. `KitosUserChangeDelivery` maps it to `ExternalUserChangeInput` and calls the application service directly. No service-to-service ingestion route or KITOS delivery account is required. The old `IsPubSubUser` field and global-admin marker routes are removed; ordinary accounts and API permissions remain intact.

The service accepts a non-empty message ID of at most 200 characters, a known organization, a non-empty external-user UUID, change type `1` (`Deleted`), and an optional UTC timestamp. Events require an enabled FK Organisation users connection. Disconnected organizations return `OperationFailure.Conflict` before deduplication or persistence, causing permanent rejection. Equivalent replays for connected organizations remain idempotent.
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

KITOS adds the `ExternalUserChanges` table with a unique source-message ID, organization/status/receipt-time index, references to the organization, matched user, and resolving user, and a concurrency version. The unmerged PubSub-user flag migrations have been removed; database changes will be regenerated from the final model without this flag. The `Organization` table gains `FkOrgUsersConnected` (default false), `FkOrgUsersConnectedAt` and `FkOrgUsersConnectedByUserId` (FK to `User`, set null on delete). These changes are in the KITOS EF Core migrations `AddExternalUserChanges` and `AddFkOrgUsersConnection`. Existing organizations start disconnected, so a local administrator must enable the users connection before PubSub deliveries are accepted.

PubSub migration `AddUserChangeReceipts` adds the receipt ledger and a `RabbitMqMigratedAt` checkpoint to the retained historical `UserChangeDeliveries` table. Apply this migration as well as the KITOS migrations. The old table is used only for offline cutover and startup reconciliation; no delivery worker polls it. Keep broker credentials in deployment-managed secret configuration; do not commit credentials or certificate private keys.

PubSub configuration:

- `UserSync:Enabled` enables publishing/ingress only; PubSub no longer registers a user-sync consumer.
- `UserSync:EnableStub` enables the development JSON publisher where allowed.
- `UserSync:QueuePrefix` defaults to `kitos.user-sync.v1`; retain it for existing backlog.
- `RabbitMQ:HostName`, `RABBIT_MQ_USER`, `RABBIT_MQ_PASSWORD`, and optional `RabbitMQ:Port`, `VirtualHost`, `TlsEnabled`, `TlsServerName` retain their existing meanings.

KITOS configuration:

- `UserSync:ConsumerEnabled` defaults to false; enables the hosted consumer and `/health/user-sync`.
- `UserSync:QueuePrefix` must match PubSub and existing main/retry/dead queues.
- `UserSync:Broker:HostName`, `UserName`, `Password` are required when enabled. Optional `Port` defaults to 5671 with TLS or 5672 without, `VirtualHost` defaults to `/`, and `TlsServerName` defaults to the broker hostname. Set `TlsEnabled=true` in production and install normal certificate trust; hostname validation is not bypassed.

KITOS on Server A initiates the private-network connection to RabbitMQ on Server B. Allow outbound/inbound broker TLS traffic between those servers. Use separate broker identities for PubSub and KITOS. Both declare the compatible topology and need configure permission for the exact queue prefix; PubSub publishes to main/dead (including offline cutover), KITOS reads main and publishes retry/dead, and quorum retry dead-lettering routes back to main. Include default-exchange routing and dead-letter permissions required by the broker; restrict access to this vhost/topology and verify effective permissions before activation. Generic PubSub authentication is unchanged.

For IIS, install Application Initialization, set the application pool `startMode=AlwaysRunning`, enable site/application `preloadEnabled`, and disable idle shutdown (`idleTimeout=00:00:00`). Ensure preload actually starts the ASP.NET Core host after machine restart; a hosted service alone cannot start an idle IIS application. Recycling closes consumer sessions; committed but unacknowledged events may redeliver safely. Verify startup without browser traffic, recycle/shutdown and reconnect in the target IIS environment before enabling production. Multiple KITOS instances may compete safely through the unique message ID constraint.

Local Compose provides independent `USER_SYNC_PUBLISHER_ENABLED`, `USER_SYNC_CONSUMER_ENABLED` and `USER_SYNC_STUB_ENABLED` switches, all defaulting to false. `TestExternalUserDeletion.ps1` requires the running KITOS consumer to be configured; it no longer requests delivery credentials.

### Cutover, replay and rollback

1. Stop ingress at the upstream/proxy and stop **all** old PubSub hosts/workers. Back up PubSub and KITOS databases and broker data. Keep source mailbox messages available for replay. Apply `AddUserChangeReceipts`; keep all historical migrations and old rows.
2. With ingress paused, run `dotnet PubSub.Application.Api.dll --migrate-user-sync-outbox` from the deployed application directory with its normal database/broker configuration. This offline mode never starts HTTP endpoints or hosted consumers. It checks every uncheckpointed legacy row in bounded batches, validates the old payload and identity, reserves the receipt, publishes pending rows to main and dead-lettered rows to `.dead` with confirmation, then atomically commits receipt acceptance and the row checkpoint. Delivered rows only seed receipts. Empty outboxes are explicitly checked and report zero transfers.
3. Rerun the same command after interruption. A confirmation followed by a failed checkpoint can produce duplicates, never silent loss. The command fails on corrupt/conflicting legacy data; investigate rather than deleting it. Verify every legacy row has `RabbitMqMigratedAt`, receipt hashes match, and main/dead queue counts account for duplicates and any authorized replay. Enabled normal startup refuses unreconciled legacy rows.
4. Deploy KITOS with its consumer disabled. Stop the old PubSub consumer and settle in-flight work before enabling KITOS on the existing queues. Deploy the publisher-only PubSub host, then enable KITOS consumption and resume ingress. Reconcile main, retry and dead-letter backlog without purging queues. Keep old rows and backups for audit. Monitor pending receipts and mailbox status; XML status 53 can stop the mailbox, so do not assume automatic source retry. Recover the dependency, coordinate mailbox restart with the provider and replay the original source message ID and event.

For pending receipts, replay the original source XML through the authenticated callback. Do not mark receipts accepted by hand: pending receipts contain no payload. For dead letters, an operator must inspect the failure, fix the cause, and republish the **same normalized body and message ID**, with `Type=kitos.user-deletion.v1`, persistent delivery and mandatory publisher confirms. Use a controlled consumer/replay client: acknowledge the dead-letter source only after confirmed routing to main. Do not send accepted dead letters back through ingress: accepted receipts intentionally short-circuit publishing. Keep production stub disabled.

For rollback, pause ingress and stop the KITOS consumer first. Before restarting an old HTTP consumer, restore the compatible KITOS binary/ingestion endpoint, restore the schema/account permissions and deployment-managed HTTP delivery configuration required by that version, and verify access. The obsolete flag migrations were unmerged and have been removed; there is no flag-removal migration to roll back. Inventory main, retry (including expired retained messages) and dead queues and reconcile every message with KITOS and legacy rows. New messages have no legacy payload row; export/reconstitute them through a reviewed offline procedure before starting an old worker. Already delivered events remain safe to replay with the original identity. Never simply start both workers or clear queues/checkpoints. Preserve the receipt ledger and historical migration; destructive downgrade/table removal is deferred until reconciliation and recovery are verified.

Container validation targets isolated RabbitMQ 4.1 and PostgreSQL instances. Transport recovery tests use an in-process delivery adapter; the persistence suite additionally exercises PubSub receipt/publish through competing KITOS consumers, committed duplicate delivery, pending review and apply/dismiss using real broker and database containers. These updated scenarios still require execution with a working container daemon. Live KOMBIT/mTLS acceptance and production cutover/rollback rehearsal remain deployment prerequisites.

### Operational checks

`/health/user-sync` on PubSub reports receipt-store and broker readiness, including pending receipt count and oldest age. KITOS registers its own endpoint when consumption is enabled and reports main/retry/dead-letter depths and active consumer count. KITOS degrades health for missing consumers or dead letters; PubSub degrades for pending receipts. Broker management/Prometheus should also monitor storage, quorum membership and ready/unacknowledged messages. The existing `Kitos.PubSub.UserSync` meter name is preserved for collector compatibility; label metrics by host/process to distinguish publication and consumption. Declaring a meter does not export metrics. Logs contain message IDs and failure categories, never event bodies or credentials.

## KOMBIT Beskedfordeler connection

### Implemented integration

The target transport is Beskedfordeler's synchronous REST push callback over mutual TLS. The generic Beskedkuvert envelope uses `ModtagBeskedInput` and contains a `Haendelsesbesked`; its extensible `Beskeddata` element does not define the payload schema for a specific producer event. The Beskedfordeler integration is therefore a separate inbound adapter in PubSub, not a change to the normalized PubSub-to-KITOS contract:

1. Beskedfordeler calls a dedicated HTTPS callback using its XML `ModtagBeskedInput`.
2. PubSub validates the envelope and authenticates the client certificate against the configured environment-specific KOMBIT/Beskedfordeler trust.
3. The adapter maps the confirmed immediate-deactivation envelope to `UserDeletionEvent` and reserves its receipt and publishes it to RabbitMQ with confirmation.
4. Only after routed broker confirmation and receipt acceptance does PubSub acknowledge with the documented XML `ModtagBeskedOutput` success (`StandardRetur.StatusKode` 20 and HTTP 200).
5. The dedicated RabbitMQ consumer forwards the normalized JSON event to KITOS; KITOS records it for local-administrator review.

The callback must use server-managed TLS certificate material, require and validate client certificates, separate test and production trust, and allow controlled certificate rotation. Beskedfordeler authentication uses client certificates. There is no outbound PubSub-to-KITOS delivery hop or delivery bearer token. The source `BeskedID` should be the idempotency key: equivalent replays can be acknowledged without creating another delivery, while reuse of an ID for a different event must not be acknowledged as successful. Confirm-and-persist-before-ack keeps the handoff durable; an acknowledgment means PubSub accepted responsibility, not that KITOS has finished delivering or an administrator has applied the change.

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

2026-10-13 local validation: the quality gate passed both solution builds with zero warnings and all unit tests. Container suites compile, but execution requires a running Docker/Podman daemon; the initial run failed during fixture startup because no runtime was detected. IIS lifecycle, Server A-to-B connectivity, production backlog reconciliation and rollback rehearsal remain unverified deployment work.


The branch adds unit coverage for event ingestion, duplicate/conflict handling, pending-change reads, apply/dismiss and batch resolution, scoped direct ingestion, confirmation sequencing and manual-ack delivery behavior, and absence of the removed ingestion route, plus receipt, cutover and real RabbitMQ/PostgreSQL coverage in `UserSyncRabbitMqTest`. Key suites are `Tests.Unit.Core.ApplicationServices`, `Tests.Unit.Presentation.Web`, `PubSub.Test`, and `Tests.Container`. `BeskedfordelerTest` covers response namespaces/codes, duplicate/conflicting IDs, failure to persist, unsupported mapping/version, disabled/unauthenticated callbacks, untrusted certificates, and bounded schema-validating XML parsing with DTD rejection. `OrgBrugerDeletionMapperTest` additionally compiles the bundled official schemas, validates a synthetic source message and success response, exercises durable mapping/replay, and rejects unsupported, ambiguous and unresolved events. Synthetic fixtures are derived from the contract, not captured provider traffic. Actual mTLS handshake tests, certificate rotation/revocation acceptance tests and end-to-end Beskedfordeler interoperability remain required before activation.

## Plan: KITOS consumes user deletions directly from RabbitMQ

### Problem / Goal

Architecture revision requested on 2026-10-13: PubSub receives and interprets KOMBIT Beskedfordeler messages, publishes normalized user-deletion events to RabbitMQ, and a KITOS-owned consumer interprets the events and invokes KITOS application services directly. Remove the service-to-service ingestion API, PubSub's KITOS login/token delivery code, and the user-level IsPubSubUser marker. This replaces the previous HTTP delivery architecture; the sections above describe the implemented direct-consumption flow.

Target flow: KOMBIT -> PubSub on Server B (authenticate, validate XML, map source event, reserve receipt) -> confirmed RabbitMQ publish on Server B -> KITOS consumer on Server A (validate contract, apply domain rules, persist) -> pending ExternalUserChange -> local administrator applies or dismisses deletion.

KITOS initiates the broker connection from Server A to Server B over the private network. PubSub interprets source XML and maps source identifiers; KITOS interprets the normalized event using its organization, identity and review rules. Keep immediate receipt conflict detection, durable publishing, at-least-once delivery, idempotency and organization opt-in. Removing the ingestion API does not remove administrator-facing review or organization-connection APIs.

### 1. Gather information

- [x] Confirm requested ownership and deployment: PubSub and RabbitMQ on Server B; KITOS on IIS on Server A owns consumption; no PubSub-to-KITOS HTTP hop.
- [x] Inspect consumer, queue contract, HTTP delivery/token providers, KITOS ingestion service and IsPubSubUser dependencies. The ingestion service can be called directly without its HTTP controller.
- [ ] Inspect KITOS startup, dependency scopes, database commit boundaries and shutdown handling. Verify an IIS-hosted consumer starts without a first web request, handles idle/recycle/shutdown and resumes after downtime. Resolve any hosting limitations before implementation.
- [x] Inventory integration-only DTOs, filters, flag queries/update methods, API routes, tests, frontend callers and deployment scripts. Preserve shared authentication and generic PubSub behavior.
- [ ] Verify deployed queue contracts, main/retry/dead-letter backlog and legacy outbox migration status. Existing messages and receipts must remain recoverable.
- [x] Implementation authorized by the user; preserve the v1 envelope and existing queue topology. Production compatibility and backlog checks remain deployment work.

### 2. Create tests and necessary validation

- [ ] Preserve PubSub callback/receipt tests: authentication, XML mapping, immediate conflicting-ID rejection, equivalent replay and confirmed routed publication before callback success.
- [x] Add KITOS consumer tests for version/message-ID/payload validation, mapping to ExternalUserChangeInput, per-delivery dependency scopes and direct service invocation without HTTP or a logged-in user.
- [ ] Test ack only after database commit or confirmed retry/dead-letter transfer. Cover crash after commit before ack, concurrent equal deliveries and conflicting IDs; only one review item may be created.
- [x] Test explicit application-result classification: malformed/unsupported events, unknown organizations, disabled users connections and conflicting IDs dead-letter; transient database/broker failures retry without silent discard.
- [ ] Use real RabbitMQ and relational-database tests for persistence, reconnect, unacked redelivery, delayed retry, failed transfers and competing KITOS instances.
- [x] Update end-to-end coverage to PubSub -> RabbitMQ -> KITOS consumer -> pending review -> apply/dismiss. Retain identity matching, unmatched identity, organization authorization, disconnection and review-concurrency tests.
- [ ] Validate flag removal on fresh and upgraded databases and removal of integration-only routes/DTO fields. Plan actual IIS lifecycle and Server A-to-B broker connectivity checks.

### 3. Implement

- [x] Keep KOMBIT callback authentication, XML validation, supported BrugerNedlagt mapping, receipt ledger and confirmed publisher in PubSub. Preserve the normalized envelope, contract version and message identity for existing queued events.
- [x] Extract the neutral wire contract and reusable broker pieces into a suitable shared boundary. KITOS must not depend on the PubSub API host to consume or deserialize events.
- [x] Move the dedicated user-change consumer into the KITOS host as a hosted service with an explicit enable switch, dedicated connection/channel, manual acknowledgements, bounded prefetch, reconnect/backoff and cancellation-aware shutdown. Resolve scoped dependencies separately per delivery.
- [x] Validate queued events and invoke ExternalUserChangeIngestionService directly. Preserve organization users-connection checks, identity matching, timestamp normalization, unique source IDs and pending administrator review. Receipt must not automatically delete user access.
- [x] Replace HTTP status handling with explicit business-result handling. Ack only after durable ingestion or confirmed routed retry/dead-letter transfer; failed transfers leave the original unacknowledged. Preserve idempotency across crash windows.
- [x] Retain durable main/retry/dead-letter topology, capped delayed retries, safe delayed return and explicit operator replay. Define compatible topology declarations and ownership; do not introduce expiry, delivery limits or overflow behavior that loses messages.
- [ ] Configure KITOS on Server A to connect to RabbitMQ on Server B with a dedicated broker identity and minimum required consume/retry/dead-letter permissions. Separate PubSub publisher permissions. Document firewall, TLS, secrets, IIS startup/idle/recycle settings and health checks.
- [x] Stop registering the user-sync consumer in PubSub. Remove KitosUserChangeDelivery, KitosTokenProvider, unused token DTOs, the user-sync HTTP client and UserSync:KitosEndpoint/KitosEmail/KitosPassword configuration. Separate producer/consumer enablement so PubSub can publish independently.
- [x] Remove ExternalUserChangeIngestionController, integration-only request/response models and RequirePubSubUserAttribute when unused. Keep the ingestion application service and administrator review/connection APIs.
- [x] Remove IsPubSubUser from the user model, mappings, response DTOs, queries, update methods, PubSub-user management routes and associated tests/UI callers. Remove the unmerged flag migrations and align the remaining designer model and snapshot; database changes will be regenerated. Do not delete actual accounts or ordinary API permissions.
- [ ] Update deployment/Compose configuration, test helpers and observability. KITOS owns consumption, ingestion, retry and dead-letter health; PubSub owns callback, receipt and publish health. Avoid logging credentials or payloads.
- [ ] Stage cutover: deploy a compatible KITOS consumer disabled; stop the PubSub consumer and settle in-flight work; enable KITOS on the existing queues; reconcile all backlog. Avoid accidentally running both delivery architectures.
- [ ] Remove the HTTP path and flag after direct delivery is verified. Document ordered rollback: restore compatible endpoint/schema/configuration before restarting the old consumer. Never purge queues; preserve outstanding legacy outbox migration/reconciliation work.
- [x] After implementation, update the current-behavior, deployment, operational and verification sections above to describe direct KITOS consumption.

### 4. Validate

- [ ] Run affected PubSub unit tests, KITOS ingestion/domain/API tests, real broker/database suites, relevant builds and repository quality checks for application changes.
- [ ] Demonstrate a valid KOMBIT event creates exactly one pending KITOS review item with no PubSub-to-KITOS HTTP call and no PubSub-marked KITOS account.
- [ ] Demonstrate KITOS downtime, IIS startup without web traffic, recycling, broker interruption and recovery without lost messages or duplicate business changes.
- [ ] Demonstrate malformed events, business conflicts, transient database failures and failed retry/dead-letter transfers; verify acknowledgements and operator replay.
- [ ] Rehearse backlog cutover and ordered rollback with main/retry/dead-letter messages. Verify PubSub's removed consumer cannot compete with KITOS.
- [ ] Verify regenerated schema migrations and absence of runtime IsPubSubUser, ingestion-route and KITOS delivery-credential dependencies. Confirm ordinary authentication and administrator authorization remain intact.
- [ ] Confirm acceptance criteria: PubSub interprets KOMBIT and durably publishes; KITOS directly consumes and interprets the normalized event; no service-to-service ingestion API or PubSub user flag remains; receipt conflicts, retries and replay are safe; organization opt-in and manual review are preserved. Production KOMBIT/certificate acceptance and broker durability checks remain deployment prerequisites.

## References

- KOMBIT, _SF1500 Organisation_, integration version 3.3; nested `SF1500 Bilag v3.2/Beskeder/Beskedtype_ORG_Bruger_1.0.pdf`, sections 1-5 (the supplied 3.3 package retains a v3.2 directory name).
- KOMBIT, _SF1461 Modtag beskeder via Beskedfordeler_, integration version 2.0.
- KOMBIT, _D.09.02 Beskedfordeler-Besked-FåTilsendt-Snitflade_ (push interface).
- KOMBIT, _D.09.03 Beskedfordeler-Besked-Hent-Snitflade_ (pull alternative, not selected).
- KOMBIT, _B.08.09 Underbilag 2O - Beskedkuvert_ and `Beskedkuvert.xsd`.
- `AnvenderSystem.wadl` from SF1461 annexes confirms POST, application/xml and HTTP 200.
- `BeskeddataBase64ver.2.00.xsd`, `Beskedfordeler.wsdl`, and related operation/value-list schemas supplied with the integration documentation.
- Main implementation: `PubSub.Application.Api/UserSync/`, `PubSub.Core.DomainModel/UserSync/`, `Core.ApplicationServices/Users/ExternalUserChange*`, `Core.DomainModel/Users/ExternalUserChange.cs`, `UserSync.Transport/`, and `Presentation.Web/Infrastructure/UserSync/`.

RabbitMQ implementation references: [Client 7.1.2 confirmation tracking](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/v7.1.2/projects/RabbitMQ.Client/CreateChannelOptions.cs), [quorum at-least-once dead-lettering](https://www.rabbitmq.com/docs/quorum-queues#dead-lettering).
