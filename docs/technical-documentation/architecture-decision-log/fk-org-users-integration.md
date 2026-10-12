# FK Organization user integration

## Status and scope

This document describes the FK Organization user-change based on the connection to KOMBIT's Beskedfordeler. The KITOS and PubSub user-change pipeline is implemented. A disabled-by-default Beskedfordeler callback and the SF1500 `ORG_Bruger` immediate-deactivation mapper are implemented with the complete SF1461 callback schema dependencies. Production activation still requires verified municipality mappings, certificate/subscription provisioning, and external-test acceptance. The current PubSub JSON stub is a development/test tool, not a production Beskedfordeler callback.

The initial supported source event is `BrugerNedlagt` (user becomes inactive), represented by the existing `Deleted` review item. This is not comprehensive detection of physical deletions: the producer explicitly emits no events for `Slet` or `Passiver`. KITOS records that report for review by the affected organization's local administrator rather than immediately removing access. The flow is designed to accept events from an upstream integration, durably hand them to KITOS, and apply or dismiss them through the internal API.

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

### Implemented integration

The target transport is Beskedfordeler's synchronous REST push callback over mutual TLS. The generic Beskedkuvert envelope uses `ModtagBeskedInput` and contains a `Haendelsesbesked`; its extensible `Beskeddata` element does not define the payload schema for a specific producer event. The Beskedfordeler integration is therefore a separate inbound adapter in PubSub, not a change to the normalized PubSub-to-KITOS contract:

1. Beskedfordeler calls a dedicated HTTPS callback using its XML `ModtagBeskedInput`.
2. PubSub validates the envelope and authenticates the client certificate against the configured environment-specific KOMBIT/Beskedfordeler trust.
3. The adapter maps the confirmed immediate-deactivation envelope to `UserDeletionEvent` and persists it in the existing durable outbox.
4. Only after the outbox commit does PubSub acknowledge with the documented XML `ModtagBeskedOutput` success (`StandardRetur.StatusKode` 20 and HTTP 200).
5. The existing delivery worker forwards the normalized JSON event to KITOS; KITOS records it for local-administrator review.

The callback must use server-managed TLS certificate material, require and validate client certificates, separate test and production trust, and allow controlled certificate rotation. It must not use the KITOS PubSub bearer token to authenticate Beskedfordeler. The bearer token remains for the outbound PubSub-to-KITOS hop only. The source `BeskedID` should be the idempotency key: equivalent replays can be acknowledged without creating another delivery, while reuse of an ID for a different event must not be acknowledged as successful. Persist-before-ack keeps the handoff durable; an acknowledgment means PubSub accepted responsibility, not that KITOS has finished delivering or an administrator has applied the change.

### Prepared receiver and configuration

`POST /user-sync/beskedfordeler` is a separate PubSub callback. It is absent operationally (404) unless `Beskedfordeler:Enabled` is true. It accepts `application/xml` or `text/xml`, requires HTTPS and a client certificate, and does not accept a bearer token as callback authentication. Certificate authorization requires both an environment-specific SHA-256 certificate fingerprint and a valid OS-trusted chain with online revocation checking and client-authentication usage. Configure the complete trust chain on the server. No forwarded-certificate HTTP header is trusted.

When enabled, Kestrel requests certificates on HTTPS connections and permits TLS 1.2/1.3. Other PubSub endpoints retain their existing authentication; the callback itself rejects missing certificates. Configure server certificates through normal Kestrel deployment configuration. TLS termination at a proxy needs a separately reviewed trust/forwarding design; this implementation expects the client certificate on the application's TLS connection.

| Configuration | Requirement |
| --- | --- |
| `Beskedfordeler:Enabled` | Defaults to false when absent. Keep disabled until municipality mappings, provisioning and external-test acceptance are complete. |
| `Beskedfordeler:SchemaDirectory` | Optional override for the complete callback XSD directory. Defaults to `UserSync/Beskedfordeler/Schemas` under the application output directory; bundled schemas are copied to build/publish output and compiled at startup. |
| `Beskedfordeler:ClientCertificateSha256` | Array of 64-character hexadecimal SHA-256 certificate fingerprints for the selected environment. Multiple entries support overlap during rotation; restart after changes. |
| `Beskedfordeler:Organizations` | Required CVR-to-KITOS-organization UUID dictionary. Keys must be eight ASCII digits; values must be non-empty and unique. Use KITOS UUIDs, not FK Organisation UUIDs. Invalid or empty mappings fail startup. |
| `UserSync:Enabled` | Must also be true, with the existing worker endpoint and credentials configured. |

For environment variables, use `Beskedfordeler__Enabled`, `Beskedfordeler__SchemaDirectory`, and `Beskedfordeler__ClientCertificateSha256__0` (and subsequent indices). Use `Beskedfordeler__Organizations__12345678=<KITOS organization UUID>` for each subscribed municipality. Verify this association against KITOS organisation records; configuration does not discover or validate remote records. Restart after changes. Trust, mappings and fingerprints must be supplied separately for test and production; no certificates, fingerprints or live municipality mappings are bundled.

The reader caps XML at 1 MiB of characters and the endpoint caps the HTTP body at 1 MiB. It prohibits DTDs and external entity resolution and validates XML against the configured schema set. All XSDs are loaded explicitly from the configured local directory; XML cannot select or download schemas. Unknown or invalid structure is rejected. The matching SF1461 callback XSDs and all their transitive dependencies are bundled under `PubSub.Application.Api/UserSync/Beskedfordeler/Schemas/`. `wadl/AnvenderSystem.wadl` is an unchanged contract reference; its original `../xsd` path corresponds to the repository's `Schemas` directory. It is not used for runtime schema resolution.

The receiver uses `Haendelsesbesked/BeskedId/sd:UUIDIdentifikator`, canonicalized as a GUID, for the existing outbox identity and accepts envelope version `1.0`. `OrgBrugerDeletionMapper` implements `IBeskedfordelerDeletionMapper` using SF1500's `Beskedtype_ORG_Bruger_1.0.pdf` (2023-12-06), sections 1-3:

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

After a successful mapping, the receiver awaits durable outbox persistence before returning `ModtagBeskedOutput` with `StandardRetur/StatusKode` 20. Equivalent normalized replays succeed; conflicting IDs return 40. XML validation failures and invalid normalized events return 40; unsupported source events, missing producer fields, unresolved CVRs, unauthorized recipient mappings and ambiguous registrations return 51, database update failures return 53, and unsupported envelope versions return 55. Protocol replies use HTTP 200 and `application/xml`; authentication and disabled-route failures use HTTP 403/404. Unexpected failures/cancellation do not produce a success acknowledgment. Payloads and XML parser diagnostics are not logged.

D.09.02 sections 5.2 and 10.2 define protocol codes 20/40/51/53/55. Section 5.2 also contains an apparent typo referring to HTTP status 20; the supplied `AnvenderSystem.wadl` explicitly specifies HTTP 200, resolving that ambiguity. The success body still carries XML status 20. The document says 40 temporarily interrupts delivery and other protocol errors stop sending from the mailbox. Restrict the subscription to supported events and configured municipalities. An unexpected event is not acknowledged or discarded; it requires operator investigation because status 51 stops delivery from the mailbox.

### Subscription and remaining deployment work

Use the `ORG_Bruger` message type and version 1.0, filtered to `ObjektHandling` UUID `9edb0c34-36e2-4d87-aba8-3ca7ade718ee`. SF1500 section 4 specifies BeskedModtag role `Modtag`, the sending authority's CVR, message type `94ef3c82-10e9-4d12-a9d9-7a7242d5ba83`, sensitivity `1d81c472-0808-44cc-963d-f5ef0170ae1d`, and municipal administrative area `*`. Verify the configured subscription in the external test environment, including that every delivered message has exactly one relevant registration.

Do not subscribe this adapter to `BrugerPlanlagtNedlagt` (`b91ebc7e-7540-44a0-a360-ea58a3adc0e2`): planned changes emit a notification at registration time, with no second notification when they take effect. Supporting them requires separate effective-date lookup/scheduling and handling later changes or cancellation. Removing a user from one organisation unit is also not interpreted as deactivating the user. `Slet` and `Passiver` emit no events according to the producer contract; comprehensive deletion detection would require a separately designed reconciliation mechanism.

Before activation, configure and verify CVR-to-KITOS UUID mappings, approve service agreements, register the callback URL/server certificate, install environment-specific trust, configure fingerprints, and create the mailbox/subscription. Run external-test delivery with a real matching SSO identity, duplicate messages, failed persistence, unknown municipalities, and certificate rotation/revocation. Confirm failure recovery so a rejected message does not indefinitely block the mailbox. The development JSON stub must not be registered as the production callback.

Pull/AMQP, value-list SOAP operations, planned-event scheduling and deletion reconciliation are out of scope for this adapter. The callback remains disabled unless explicitly enabled.

## Verification coverage

The branch adds unit coverage for event ingestion, duplicate/conflict handling, pending-change reads, apply/dismiss and batch resolution, PubSub token handling and delivery behavior, integration endpoint authentication, plus persistence coverage for the PubSub outbox. Key suites are `Tests.Unit.Core.ApplicationServices`, `Tests.Unit.Presentation.Web`, `PubSub.Test`, and `Tests.Container`. `BeskedfordelerTest` covers response namespaces/codes, duplicate/conflicting IDs, failure to persist, unsupported mapping/version, disabled/unauthenticated callbacks, untrusted certificates, and bounded schema-validating XML parsing with DTD rejection. `OrgBrugerDeletionMapperTest` additionally compiles the bundled official schemas, validates a synthetic source message and success response, exercises durable mapping/replay, and rejects unsupported, ambiguous and unresolved events. Synthetic fixtures are derived from the contract, not captured provider traffic. Actual mTLS handshake tests, certificate rotation/revocation acceptance tests and end-to-end Beskedfordeler interoperability remain required before activation.

## References

- KOMBIT, _SF1500 Organisation_, integration version 3.3; nested `SF1500 Bilag v3.2/Beskeder/Beskedtype_ORG_Bruger_1.0.pdf`, sections 1-5 (the supplied 3.3 package retains a v3.2 directory name).
- KOMBIT, _SF1461 Modtag beskeder via Beskedfordeler_, integration version 2.0.
- KOMBIT, _D.09.02 Beskedfordeler-Besked-FåTilsendt-Snitflade_ (push interface).
- KOMBIT, _D.09.03 Beskedfordeler-Besked-Hent-Snitflade_ (pull alternative, not selected).
- KOMBIT, _B.08.09 Underbilag 2O - Beskedkuvert_ and `Beskedkuvert.xsd`.
- `AnvenderSystem.wadl` from SF1461 annexes confirms POST, application/xml and HTTP 200.
- `BeskeddataBase64ver.2.00.xsd`, `Beskedfordeler.wsdl`, and related operation/value-list schemas supplied with the integration documentation.
- Main implementation: `PubSub.Application.Api/UserSync/`, `PubSub.Core.DomainModel/UserSync/`, `Core.ApplicationServices/Users/ExternalUserChange*`, `Core.DomainModel/Users/ExternalUserChange.cs`, and `Presentation.Web/Controllers/API/V2/Integration/ExternalUserChangeIngestionController.cs`.
