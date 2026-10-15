---
name: kitos-pubsub-events
description: "Add, modify, or review KITOS domain events that publish to PubSub. Use for event topics, payload contracts, publishing handlers, and transaction-safe notification delivery."
---

# KITOS PubSub events

## Find the existing pattern

Read `Core.DomainModel/Events`, `Core.ApplicationServices/KitosEvents`, and
`Core.ApplicationServices/Model/KitosEvents`. Trace the mutation service, its
callers (including archiving and cascades), transaction scopes, and tests.

| Domain trigger | PubSub topic |
|---|---|
| System/DPR snapshot changes in `PublishSystemChangesEventHandler` | `KitosITSystemChangedEvent` |
| `SystemTakenIntoUsageEvent` | `KitosItSystemUsageCreatedEvent` |
| `SystemTakenOutOfUsageEvent` | `KitosItSystemUsageDeletedEvent` |

Both usage payloads contain exactly `SystemUuid` and `OrganizationUuid`. The
system-change payload contains `SystemUuid` and only the changed name or
data-processor fields; inspect its mapper for omitted/null semantics.

## Define the contract

- Establish the topic, exact payload fields, triggering operations, commit
  timing, and delivery guarantee before adding an event.
- Treat topic strings and serialized keys as public contracts. Preserve omitted
  versus null fields. Do not rename a topic when renaming a domain event.
- Name domain events after business actions, not generic deletion/cleanup.
- Use a dedicated `IEventBody` with `ToKeyValuePairs()`, a topic constant in
  `KitosQueueTopics`, and a thin `IDomainEventHandler<T>` that creates `KitosEvent`.
- Reuse `IKitosEventPublisherService`; do not add direct HTTP/RabbitMQ calls to
  domain entities or business services.
- Register the handler with `RegisterDomainEvent<THandler>` in
  `Presentation.Web/Infrastructure/DI/KitosServiceRegistration.cs`.

## Respect transaction boundaries

- Raise domain events from application services after authorization.
- Put shared behavior at the common mutation path so APIs, archiving, and
  cascading operations behave consistently.
- Capture immutable UUID values before deletion or cleanup changes navigation
  properties. Do not retain a deleted entity for deferred payload mapping.
- If notification means the change is committed, use
  `transaction.AfterCommit(() => domainEvents.Raise(domainEvent))` before
  committing. A nested `Commit()` is not an actual database commit.
- Committed nested scopes transfer callbacks to their parent; rollback or
  uncommitted disposal discards them. This requires `ITransactionManager`-owned
  transactions.
- Preserve generic pre-deletion events used by cleanup, tracking, and read models.
- Only usage deletion currently uses outermost-commit callbacks; inspect the
  existing timing before changing creation or system-change publication.
- Current delivery is best-effort: log returned failures, non-success HTTP
  responses, and exceptions without undoing committed changes. Durable retries
  require an explicitly agreed outbox design.

## Prove behavior

- Add handler tests for the literal topic string, exact serialized field set,
  values, omitted/null behavior, and one publication attempt per event.
- Add service tests for success, authorization/not-found/error paths, identity
  snapshots, and deferred dispatch.
- Cover nested commit, multiple callbacks, rollback, uncommitted disposal, and
  commit failure when changing transaction behavior.
- Extend `Tests.Container/Tests/PubSubTest.cs` for new topics. Use PostgreSQL
  transaction coverage in `PostgreSqlTransactionTest`; these require Docker/Podman.
- Run focused tests, then `pwsh .\scripts\quality-check.ps1` with tests enabled,
  and self-review using the `code-quality` skill.
