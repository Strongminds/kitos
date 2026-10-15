---
name: kitos-pubsub-events
description: "Add, modify, or review KITOS domain events that publish to PubSub. Use for event topics, payload contracts, publishing handlers, and transaction-safe notification delivery."
---

# KITOS PubSub events

## Read the existing path

- Start with `docs/architectural-decisions/0001-kitos-pubsub-events.md`.
- Inspect `Core.DomainModel/Events`, `Core.ApplicationServices/KitosEvents`,
  and `Core.ApplicationServices/Model/KitosEvents`.
- Trace the initiating service, all callers (including archive/cascade paths),
  transaction scopes, handler DI registration, and existing tests before editing.
- Keep domain-event names distinct from generic lifecycle cleanup events.
  `SystemTakenIntoUsageEvent` and `SystemTakenOutOfUsageEvent` describe business
  actions; the latter still publishes `KitosItSystemUsageDeletedEvent`.

## Define the contract

- Confirm the topic, exact payload fields, triggering operations, commit timing,
  and delivery guarantee before implementing a new event.
- Treat topic strings and serialized keys as public contracts. Preserve omitted
  versus null fields. Do not rename a topic when renaming a domain event.
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
- The transaction manager transfers nested callbacks to the parent only when
  that scope commits; rollback/uncommitted disposal discards them. Use managed
  transactions, not raw EF transactions, for this callback path.
- Preserve generic pre-deletion events used by cleanup, tracking, and read models.
- Do not imply all existing events are outermost-commit-aware: creation and
  system-change publication retain their existing timing.
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
- Update the architecture decision's event catalog when contracts change.
- Run focused tests, then `pwsh .\scripts\quality-check.ps1` with tests enabled,
  and self-review using the `code-quality` skill.
- If NuGet audit warnings block the gate, trace transitive packages with
  `dotnet nuget why`, update to verified patched servicing releases, and validate
  both solutions. Never disable audits, suppress warnings, or relax the gate.
