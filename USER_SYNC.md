# User synchronization

## Implementation status

Implemented: normalized KITOS ingestion and read APIs; persisted PubSub stub deliveries with retries; administrator Apply/Dismiss and batch resolution; organization role removal; a pending-count endpoint for future UI notifications.

The UI is deferred at the user's request; this task leaves no frontend source changes. Also not implemented: the real Beskedfordeler subscription API, source consumer/parser, and connect/disconnect backend. The supplied analysis has no subscription wire contract or representative deletion envelope. Do not treat the normalized stub contract below as a Beskedfordeler message format. Live testing and pilot rollout remain release prerequisites.

The [official integration guide](https://digitaliseringskataloget.dk/bliv-klar-til-kode-beskedfordeler) describes subscription/mailbox setup in the administration UI and a SOAP value-list editing service for dynamic filtering. The planned automatic subscription lifecycle therefore needs a confirmed contract or a decision to manage CVR filters through provisioned value lists; a direct create/delete subscription API has not been established.

## Configuration and migrations

Apply the KITOS `AddExternalUserChanges` and `AddPubSubUser` migrations and PubSub `AddUserChangeDeliveries` migration using the normal deployment workflow. The migration preserves existing users and roles; Apply removes the affected organization role assignments. An unrelated existing `SupplierAssociatedFieldConfiguration.FieldKey` model/snapshot discrepancy is deliberately excluded from these migrations.

KITOS account setup: create a dedicated API user with ordinary organization membership and `HasApiAccess=true`. A global administrator grants `IsPubSubUser` through `PATCH /api/v2/internal/users/pubsub-users/{userUuid}?requestedValue=true` (use `false` to revoke). `GET /api/v2/internal/users/pubsub-users` lists assigned accounts. No global-admin or system-integrator status is required for the PubSub account. The flag defaults to false for existing users.

PubSub configuration:

```
UserSync__Enabled=true
UserSync__KitosEndpoint=https://<KITOS>/api/v2/integrations/fk-organisation/user-changes
UserSync__KitosEmail=<dedicated KITOS API user email>
UserSync__KitosPassword=<password from secret storage>
UserSync__EnableStub=true
```

The stub additionally requires the ASP.NET environment to be Development, Local, or Test, and rejects an explicit Production deployment environment. Disable `EnableStub` outside test environments. No secrets are checked into this repository. Use trusted HTTPS certificates. The worker obtains a KITOS token through `/api/authorize/GetToken`, caches it in memory, renews one minute before expiry, and renews once after a 401. Login uses the same origin as KitosEndpoint, with the token route at the server root. Missing credentials stop an enabled worker; login failures leave deliveries retryable. Provision a PubSub publisher token using the existing authentication mechanism.

## Contracts

`POST /api/v2/integrations/fk-organisation/user-changes` accepts this UTF-8 JSON envelope:

```json
{
  "Payload": {
    "ExternalMessageId": "unique-message-id",
    "OrganizationUuid": "11111111-1111-1111-1111-111111111111",
    "ExternalUserUuid": "22222222-2222-2222-2222-222222222222",
    "ChangeType": 1,
    "OccurredAt": null
  }
}
```

Send `Authorization: Bearer <KITOS token>`. `[IntegrationApi]` requires bearer authentication and excludes these endpoints from Swagger. Ingestion additionally checks the current database user: active, able to authenticate, API access enabled, and `IsPubSubUser=true`. Revoking the flag blocks existing tokens. PubSub users may ingest for any known organization; their account membership enables login and does not restrict event routing. Other integration endpoints must define their own permissions. `ChangeType=1` means Deleted. Optional occurrence time is UTC. `OrganizationUuid` is the KITOS organization UUID; source routing must resolve the external organization context before enqueueing. User UUID is resolved through `ISsoUserIdentityRepository`, with separate membership validation. An absent/wrong-organization binding creates an unmatched pending record. No email lookup is performed.

Successful insertion and identical replay return 200 with the change UUID. Invalid payloads return 400, missing/invalid tokens 401, insufficient account permissions 403, unknown organizations 404, and conflicting reuse of an ID 409. An ID is globally unique and must be stable across redelivery. Existing Applied/Dismissed records are never reset by replay.

Administrator API prefix: `/api/v2/internal/organization/{organizationUuid}/users/external-changes`.

- `GET`: `status`, `resolvedOnly`, `skip`, `take` (1–200), `search`, `sort` (`receivedAt`, `userName`, `status`), `descending`. Returns `total`, `pendingCount`, and `items`.
- `GET /{changeUuid}`: detail.
- `GET /pending-count`: local-admin notification count without returning user details.
- `POST /{changeUuid}/apply` and `/dismiss`: resolve a Pending change; repeat resolution returns conflict.
- `POST /resolve`: `{ "changeUuids": ["..."], "apply": true }`, up to 100 selected IDs. Returns per-item `uuid`, `success`, and `error`. Each item commits independently.

All administrator operations require a local-admin role in that organization. V2 responses serialize enums as names. Apply rejects global administrator targets because an organization-scoped operation cannot revoke global authority. Missing/remapped identities cannot be applied. Apply resolves a previously unmatched record if a valid binding now exists.

## Access semantics

Apply removes the user's organization role assignments and commits the audit resolution in one serializable transaction. Membership requires the User role. If no User role remains in another organization, Apply sets Deleted and DeletedDate on the existing user; it preserves the user record, identity binding, and changelog. Other organizations remain accessible. There is no separate deactivation table or query filter. SSO retains its existing behavior and may assign organization roles again on a subsequent login. Applied deletion history does not block SSO reuse. Administrative-access cache invalidation follows a successful Apply.

## Reproducible test scenarios

1. Select an existing test `SsoUserIdentity.ExternalUuid` whose KITOS user belongs to the test organization. Set `USER_SYNC_PUBLISH_TOKEN` in the environment.
2. Run `DeploymentScripts/TestUserSync.ps1 -PubSubBaseUrl https://<pubsub> -OrganizationUuid <uuid> -ExternalUserUuid <uuid> -Duplicate`. The script asserts both commands return the same durable delivery ID. Verify one pending change through the KITOS list endpoint and unchanged access.
3. Repeat with an unknown external UUID and a new message ID. Verify an unmatched pending record; Apply fails safely and Dismiss succeeds.
4. In an isolated test environment, make KITOS unavailable, enqueue an event, then restore KITOS. Verify the same message eventually arrives once. Restart PubSub during the outage to verify persisted recovery.
5. Have two administrators resolve the same change concurrently; only one succeeds. Apply in organization A and verify access in B still works and subsequent SSO login follows the existing role-assignment flow.

Targeted automated checks:

```powershell
dotnet test Tests.Unit.Core.ApplicationServices --filter 'FullyQualifiedName~ExternalUserChangeIngestionServiceTest|FullyQualifiedName~SSO'
dotnet test Tests.Unit.Presentation.Web --filter 'FullyQualifiedName~ExternalUserChangeIngestionControllerTest|FullyQualifiedName~IntegrationApiAuthenticationTest'
dotnet test PubSub.Test --filter 'FullyQualifiedName~UserChangeDeliveryTest|FullyQualifiedName~KitosTokenProviderTest'
dotnet test Tests.Container --filter FullyQualifiedName~UserSyncPersistenceTest
```

The container tests use an isolated PostgreSQL instance and require Docker or Podman. They exercise database uniqueness, parallel insertion, transactional Apply/Dismiss races, role removal and last-organization soft deletion, unmatched users, and organization boundaries.

Verified locally on 2026-09-28 after the role-removal update: 120 selected application authorization/SSO/ingestion tests, 8 callback-controller tests, and 6 PostgreSQL persistence tests passed. The 9 PubSub delivery tests passed before the bearer-authentication change described above; see the new targeted checks for current validation. The PostgreSQL group also exercises the new KITOS migrations in both directions. No migrations were applied to a deployed database. Live Beskedfordeler delivery, production rollout, and the deferred frontend have not been validated.

Bearer-authentication update validation: 44 user-write tests, 5 ingestion/controller pipeline tests, and 14 token-provider/delivery tests passed. The pipeline checks token-only access, immediate flag revocation, account eligibility, and Swagger exclusion. The updated PostgreSQL migration round-trip test could not run because no container runtime was active; it remains a deployment prerequisite.

## Delivery operations and remaining integration work

`UserChangeDeliveries` is the durable handoff. A future source consumer must acknowledge only after `UserChangeOutbox.Enqueue` commits. The worker leases records with optimistic concurrency, uses a 30-second HTTP timeout and a 90-second total delivery timeout including login/renewal, retries transient failures with backoff up to five minutes, and recovers expired two-minute leases after restart. Duplicates after a crash are safe at KITOS. The existing generic RabbitMQ consumers are unchanged.

Monitor undelivered rows, their attempt count/next-attempt time, `LastStatusCode`, and `DeadLettered`. Permanent 4xx payload/routing conflicts become dead letters; 401/403, 408, 429, network errors, and server errors remain retryable. Logs contain delivery IDs and HTTP status, not payloads or credentials. After fixing a permanent configuration/payload issue, an operator must explicitly redrive or correct the event; the stub does not reset existing dead-letter records.

Before implementing the real source adapter, obtain the subscription create/delete contract, credentials/certificates, transport details, a representative deletion envelope including BeskedID and organization routing fields, and the unsupported-event policy. Implement and test that adapter against the same durable outbox, then add connection controls and test the real deletion-to-review flow with a Ballerup test identity before piloting.
