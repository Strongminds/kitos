# ADR 001: Receive user-change messages from Kombit's Beskedfordeler

- **Status:** Proposed
- **Date:** 2026-10-05

## Context

KITOS PubSub already has a durable outbound user-change flow:

1. A `UserDeletionEvent` is enqueued in `PubSubContext.UserChangeDeliveries`.
2. `UserChangeDeliveryWorker` sends the stored JSON payload to KITOS using a KITOS PubSub-user token.
3. KITOS validates that user and records the incoming event as a pending external user change.

`UserSyncStubController` currently exposes a development-only JSON endpoint for creating those normalized events. It is not a Beskedfordeler integration. The Beskedfordeler specifications describe a synchronous REST push callback over mutual TLS, carrying an XML `ModtagBeskedInput` that contains a `Haendelsesbesked`.

The generic Beskedkuvert schema defines the outer envelope and permits extensible `Beskeddata`. It does not define the data schema for any specific message type. The producer-specific schema or representative message for the intended user-deletion event has not yet been supplied.

## Decision

Implement Beskedfordeler push reception as a distinct inbound adapter in the PubSub API. Keep the existing normalized event and outbound PubSub-to-KITOS delivery path as the downstream contract.

### Inbound transport and authentication

- Expose a production HTTPS REST callback reachable by Beskedfordeler and register its exact endpoint URL and endpoint certificate in the relevant administration module.
- Terminate TLS 1.2 at Kestrel so the application can inspect the peer certificate. Configure the callback's HTTPS server certificate through deployment-managed certificate material; do not check private keys or passwords into source control.
- Require a client certificate and validate it against Beskedfordeler's trusted, environment-specific FOCES functional certificate. Configure test and production trust separately and support certificate rotation without accepting arbitrary client certificates.
- Do not authenticate the inbound callback using the KITOS PubSub user's bearer token. That account remains exclusively for PubSub-to-KITOS delivery.

### XML callback contract

- Add a dedicated callback route and XML request/response DTOs for the Beskedfordeler `ModtagBeskedInput` and `ModtagBeskedOutput` structures. Do not reuse the JSON development stub as the production callback contract.
- Validate the event envelope against the applicable Beskedkuvert version/schema, including required structure, version, identifiers, and safe XML parser settings. Reject malformed or unsupported messages without returning a success acknowledgment.
- Map `Haendelsesbesked.BeskedId` to the inbound idempotency key. Map the producer's deletion data to the existing normalized event only after the producer-specific `Beskeddata` schema and field meanings have been confirmed.
- Commit the accepted normalized event to the durable outbox before acknowledging it. Return the XML `ModtagBeskedOutput` with `StandardRetur.StatusKode` 20 and HTTP 200 only after persistence succeeds.
- On persistence or transient processing failure, do not return the success acknowledgment. Beskedfordeler keeps the message in the mailbox and retries delivery. Follow its documented response/status behavior for malformed, unsupported, or permanently invalid messages; do not silently acknowledge messages that cannot be represented or safely processed.

### Idempotency and downstream delivery

- Persist the Beskedfordeler `BeskedID` as the unique source-message identity and make concurrent duplicate delivery safe.
- A replay with the same ID and equivalent event is acknowledged without creating a second downstream delivery. Reuse of the same ID for a different event is treated as a conflict and is not acknowledged as successful.
- Preserve the existing outbound delivery worker's retry, lease/concurrency, dead-letter, and KITOS token behavior. The downstream request remains the normalized KITOS event envelope, not Beskedfordeler's XML.
- Keep the outbox durable as the handoff boundary: Beskedfordeler acknowledgement means PubSub has safely accepted responsibility, not that KITOS has already completed delivery.

### Scope boundaries

- Initially handle the user-deletion message type only.
- Do not add Beskedfordeler pull/AMQP support or value-list SOAP operations as part of this inbound push connection.
- Assume Kombit's Beskedfordeler-side mailbox, subscription, service agreement, and permissions are provisioned correctly. Deployment must still configure the callback URL and certificates required by the documented handshake.
- Keep the existing KITOS ingestion authorization and business behavior unchanged unless mapping validation reveals a necessary contract change.

## Consequences

- PubSub gains a secure XML/mTLS inbound edge while retaining a decoupled, retryable outbound handoff to KITOS.
- Beskedfordeler can redeliver after network or service failures; unique `BeskedID` handling prevents duplicate outbox work.
- The service must remain available and process messages promptly to avoid mailbox buildup. Operations need visibility into rejected messages, repeated failures, and dead-lettered outbound deliveries without logging sensitive message data.
- Test and production environments require separate Beskedfordeler FOCES trust material and appropriate server-certificate registration.
- The event-specific mapping is not implementable from the generic envelope specifications alone. It is blocked until the producer's `Beskeddata` schema, message-type identity, and deletion-field semantics are provided.

## Implementation and verification plan

1. Obtain the producer-specific message specification or an authoritative deletion-message sample. Confirm the Beskedtype UUID, organization identity mapping, external user identity mapping, event timestamp semantics, and any optional/required data fields.
2. Add XML envelope parsing/schema validation and a dedicated mapper, with tests for valid messages, malformed XML, unsupported versions/types, missing fields, and invalid identifiers.
3. Add the mTLS callback endpoint and deployment-managed Kestrel HTTPS configuration. Test missing, untrusted, wrong-environment, and accepted client certificates; verify TLS 1.2 and certificate-rotation configuration.
4. Add durable storage keyed by `BeskedID` and the XML acknowledgment response. Test that persistence precedes HTTP 200/`StatusKode` 20, persistence failures do not acknowledge, equivalent duplicates are harmless, and conflicting IDs are rejected.
5. Retain and test the existing PubSub-to-KITOS retry path, including end-to-end verification that accepted Beskedfordeler data is delivered in the current KITOS event envelope.
6. Verify deployment settings and callback certificate registration for test and production, then run the targeted PubSub and KITOS test suites.

## References

- KOMBIT, *SF1461 Modtag beskeder via Beskedfordeler*, integration version 2.0.
- KOMBIT, *D.09.02 Beskedfordeler-Besked-FåTilsendt-Snitflade*.
- KOMBIT, *D.09.03 Beskedfordeler-Besked-Hent-Snitflade* (not selected; pull alternative).
- KOMBIT, *B.08.09 Underbilag 2O - Beskedkuvert* and `Beskedkuvert.xsd`.
- `BeskeddataBase64ver.2.00.xsd`, `Beskedfordeler.wsdl`, and related operation/value-list schemas supplied with the integration documentation.
- Existing implementation: `PubSub.Application.Api/UserSync/UserSyncStubController.cs`, `UserChangeOutbox.cs`, `UserChangeDeliveryWorker.cs`, and `Presentation.Web/Controllers/API/V2/Integration/ExternalUserChangeIngestionController.cs`.
