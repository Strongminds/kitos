# FK Organization user integration

## Status and scope

This document describes the FK Organization user-change based on the connection to KOMBIT's Beskedfordeler. The KITOS and PubSub user-change pipeline is implemented. A disabled-by-default Beskedfordeler callback foundation is implemented, but **production deletion mapping is not implemented** and the supplied schema bundle is incomplete. The current PubSub JSON stub is a development/test tool, not a production Beskedfordeler callback.

The initial supported change is an externally reported user deletion. KITOS records that report for review by the affected organization's local administrator rather than immediately removing access. The flow is designed to accept events from an upstream integration, durably hand them to KITOS, and apply or dismiss them through the internal API.

## End-to-end flow

```text
External source
    -> PubSub durable delivery outbox
    -> HTTPS KITOS integration API
    -> pending ExternalUserChange
    -> organization's local administrator reviews it
    -> apply deletion or dismiss
```

The normalized event is deliberately independent of any particular source transport. KOMBIT XML must be translated to this contract by a producer-specific mapper in the Beskedfordeler adapter; the KITOS ingestion API does not accept KOMBIT XML.

### 1. PubSub event and durable outbox

`PubSub.Core.DomainModel.UserSync.UserDeletionEvent` is the normalized event:

| Field               | Meaning                                                                |
| ------------------- | ---------------------------------------------------------------------- |
| `ExternalMessageId` | Stable source-message identity, unique in the PubSub outbox and KITOS. |
| `OrganizationUuid`  | KITOS UUID of the organization whose access is affected.               |
| `ExternalUserUuid`  | External/SAML identity UUID used to find the KITOS user.               |
| `ChangeType`        | `1` (`Deleted`) is currently the only supported value.                 |
| `OccurredAt`        | Optional UTC event time.                                               |

The PubSub `UserChangeOutbox` serializes the event in a `{ "Payload": ... }` envelope and persists it in `UserChangeDeliveries` before returning success to its caller. Replaying the same message ID and identical serialized event returns the existing delivery; reusing that ID for a different event is a conflict. The outbox table has a unique index on `ExternalMessageId`.

`UserChangeDeliveryWorker` is enabled only when `UserSync:Enabled` is true. It requires an HTTPS `UserSync:KitosEndpoint`, obtains a normal KITOS token using `UserSync:KitosEmail` and `UserSync:KitosPassword`, and posts the stored JSON envelope with a bearer token. Token credentials are sent to the authorization endpoint on the same host as the configured ingestion endpoint; tokens are cached in memory and refreshed after expiry or a 401 response. The worker retries once after a 401, uses a two-minute persisted lease for concurrent workers, limits each batch to 25 records, and times out an individual attempt after 90 seconds. Failed deliveries are rescheduled with exponential backoff, up to 256 seconds. Permanent client errors are dead-lettered, with explicit retry exceptions for request timeout, throttling, unauthorized, and forbidden responses. Payloads are not written to worker logs.

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

Events are accepted only for organizations whose FK Organisation **users connection** is enabled (see below). For any other organization the endpoint returns `409 Conflict` before deduplication or persistence. 409 was chosen over 422 because the request is well-formed but conflicts with the organization's current state, it maps to the existing `OperationFailure.Conflict`, and it is a permanent client error that `UserChangeDeliveryWorker` dead-letters instead of retrying (unlike 401/403/408/429). Identical replays for connected organizations remain idempotent.

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

PubSub stores pending outbound work separately in its own `UserChangeDeliveries` table. Apply the corresponding PubSub migration as well as the KITOS migrations when deploying the feature. Keep PubSub's KITOS account credentials in deployment-managed secret configuration; do not commit credentials or certificate private keys.

Relevant PubSub configuration keys are:

- `UserSync:Enabled` — run the delivery worker.
- `UserSync:KitosEndpoint` — HTTPS KITOS ingestion URL.
- `UserSync:KitosEmail` and `UserSync:KitosPassword` — KITOS PubSub account credentials.
- `UserSync:EnableStub` — explicitly enable the test-only JSON publisher where allowed.

The PubSub worker will not start delivery if disabled and fails startup validation if enabled without an HTTPS endpoint or the required account credentials. The Beskedfordeler callback has separate opt-in configuration described below; actual environment certificates, trust installation, and callback provisioning remain deployment work.

## KOMBIT Beskedfordeler connection

### Proposed integration

The target transport is Beskedfordeler's synchronous REST push callback over mutual TLS. The generic Beskedkuvert envelope uses `ModtagBeskedInput` and contains a `Haendelsesbesked`; its extensible `Beskeddata` element does not define the payload schema for a specific producer event. The Beskedfordeler integration is therefore a separate inbound adapter in PubSub, not a change to the normalized PubSub-to-KITOS contract:

1. Beskedfordeler calls a dedicated HTTPS callback using its XML `ModtagBeskedInput`.
2. PubSub validates the envelope and authenticates the client certificate against the configured environment-specific KOMBIT/Beskedfordeler trust.
3. The adapter maps the confirmed user-deletion payload to `UserDeletionEvent` and persists it in the existing durable outbox.
4. Only after the outbox commit does PubSub acknowledge with the documented XML `ModtagBeskedOutput` success (`StandardRetur.StatusKode` 20 and HTTP 200).
5. The existing delivery worker forwards the normalized JSON event to KITOS; KITOS records it for local-administrator review.

The callback must use server-managed TLS certificate material, require and validate client certificates, separate test and production trust, and allow controlled certificate rotation. It must not use the KITOS PubSub bearer token to authenticate Beskedfordeler. The bearer token remains for the outbound PubSub-to-KITOS hop only. The source `BeskedID` should be the idempotency key: equivalent replays can be acknowledged without creating another delivery, while reuse of an ID for a different event must not be acknowledged as successful. Persist-before-ack keeps the handoff durable; an acknowledgment means PubSub accepted responsibility, not that KITOS has finished delivering or an administrator has applied the change.

### Prepared receiver and configuration

`POST /user-sync/beskedfordeler` is a separate PubSub callback. It is absent operationally (404) unless `Beskedfordeler:Enabled` is true. It accepts `application/xml` or `text/xml`, requires HTTPS and a client certificate, and does not accept a bearer token as callback authentication. Certificate authorization requires both an environment-specific SHA-256 certificate fingerprint and a valid OS-trusted chain with online revocation checking and client-authentication usage. Configure the complete trust chain on the server. No forwarded-certificate HTTP header is trusted.

When enabled, Kestrel requests certificates on HTTPS connections and permits TLS 1.2/1.3. Other PubSub endpoints retain their existing authentication; the callback itself rejects missing certificates. Configure server certificates through normal Kestrel deployment configuration. TLS termination at a proxy needs a separately reviewed trust/forwarding design; this implementation expects the client certificate on the application's TLS connection.

| Configuration | Requirement |
| --- | --- |
| `Beskedfordeler:Enabled` | Defaults to false when absent. Keep disabled until the producer mapper, full schemas, and acceptance testing are complete. |
| `Beskedfordeler:SchemaDirectory` | Local directory containing the complete callback XSD set and dependencies. Loaded and compiled at startup. |
| `Beskedfordeler:ClientCertificateSha256` | Array of 64-character hexadecimal SHA-256 certificate fingerprints for the selected environment. Multiple entries support overlap during rotation; restart after changes. |
| `UserSync:Enabled` | Must also be true, with the existing worker endpoint and credentials configured. |

For environment variables, use `Beskedfordeler__Enabled`, `Beskedfordeler__SchemaDirectory`, and `Beskedfordeler__ClientCertificateSha256__0` (and subsequent indices). Trust and fingerprints must be supplied separately for test and production; no certificates or fingerprints are bundled.

The reader caps XML at 1 MiB of characters and the endpoint caps the HTTP body at 1 MiB. It prohibits DTDs and external entity resolution and validates XML against the configured schema set. All XSDs are loaded explicitly from the configured local directory; XML cannot select or download schemas. Unknown or invalid structure is rejected. The original supplied callback XSDs are kept under `PubSub.Application.Api/UserSync/Beskedfordeler/Schemas/` as reference assets, not as a complete deployable schema set.

The receiver uses `Haendelsesbesked/BeskedId/sd:UUIDIdentifikator`, canonicalized as a GUID, for the existing outbox identity. It accepts envelope version `1.0`. `IBeskedfordelerDeletionMapper` is the explicit producer-contract boundary. Its current implementation deliberately returns unsupported for every payload. A confirmed mapper must validate the producer message type and payload and map the KITOS organization, external identity and UTC occurrence timestamp deterministically. It must not interpret envelope actors or object IDs as those identities without producer-specific evidence.

After a successful mapping, the receiver awaits durable outbox persistence before returning `ModtagBeskedOutput` with `StandardRetur/StatusKode` 20. Equivalent normalized replays succeed; conflicting IDs return 40. XML validation failures and invalid mappings return 40, unsupported mappings return 51, database update failures return 53, and unsupported envelope versions return 55. Protocol replies use HTTP 200 and `application/xml`; authentication and disabled-route failures use HTTP 403/404. Unexpected failures/cancellation do not produce a success acknowledgment. Payloads and XML parser diagnostics are not logged.

D.09.02 sections 5.2 and 10.2 define protocol codes 20/40/51/53/55. Section 5.2 also contains an apparent typo referring to HTTP status 20; HTTP 200 with XML status 20 follows the architecture decision, but must be verified in the external test environment. The document says 40 temporarily interrupts delivery and other protocol errors stop sending from the mailbox. Do not enable the unsupported mapper against a live subscription.

### Not yet implemented / blocking contract details

The producer-specific mapper and live interoperability testing remain incomplete. The supplied XSDs reference missing `1.1/SagDokObjekt.xsd`, `1.1/OrganisationFaelles.xsd`, and `cached/xmldsig-core-schema.xsd` (plus any transitive dependencies). Obtain the matching complete schema release; do not substitute permissive schemas. Startup rejects incomplete schemas. `Anvendersystem.wadl`, referenced by the push specification, was not supplied and should also be obtained to confirm the callback contract.

The producer-specific deletion-message specification or an authoritative sample is needed before that mapping can be safely implemented. Confirm the message-type UUID, organization identifier mapping, external user identity mapping, timestamp meaning, and required/optional fields. The generic Beskedkuvert schema alone is insufficient. Confirm the chosen failure responses and operational handling of permanently invalid messages in an interoperability test so a rejected message does not indefinitely block the mailbox. The development JSON stub must not be registered as the production callback.

The intended first iteration is deletion messages only. Beskedfordeler pull/AMQP support and value-list SOAP operations are out of scope. Mailbox/subscription/service-agreement provisioning and callback URL/certificate registration are external deployment prerequisites.

## Verification coverage

The branch adds unit coverage for event ingestion, duplicate/conflict handling, pending-change reads, apply/dismiss and batch resolution, PubSub token handling and delivery behavior, integration endpoint authentication, plus persistence coverage for the PubSub outbox. Key suites are `Tests.Unit.Core.ApplicationServices`, `Tests.Unit.Presentation.Web`, `PubSub.Test`, and `Tests.Container`. `BeskedfordelerTest` covers response namespaces/codes, duplicate/conflicting IDs, failure to persist, unsupported mapping/version, disabled/unauthenticated callbacks, untrusted certificates, and bounded schema-validating XML parsing with DTD rejection. Its small parser schema is a test fixture, not a substitute for the official schemas. Full official-schema/producer fixtures, actual mTLS handshake tests, certificate rotation/revocation acceptance tests, and end-to-end Beskedfordeler interoperability remain required before activation.

## References

- KOMBIT, _SF1461 Modtag beskeder via Beskedfordeler_, integration version 2.0.
- KOMBIT, _D.09.02 Beskedfordeler-Besked-FåTilsendt-Snitflade_ (push interface).
- KOMBIT, _D.09.03 Beskedfordeler-Besked-Hent-Snitflade_ (pull alternative, not selected).
- KOMBIT, _B.08.09 Underbilag 2O - Beskedkuvert_ and `Beskedkuvert.xsd`.
- `BeskeddataBase64ver.2.00.xsd`, `Beskedfordeler.wsdl`, and related operation/value-list schemas supplied with the integration documentation.
- Main implementation: `PubSub.Application.Api/UserSync/`, `PubSub.Core.DomainModel/UserSync/`, `Core.ApplicationServices/Users/ExternalUserChange*`, `Core.DomainModel/Users/ExternalUserChange.cs`, and `Presentation.Web/Controllers/API/V2/Integration/ExternalUserChangeIngestionController.cs`.
