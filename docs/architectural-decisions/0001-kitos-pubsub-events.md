# ADR 0001: KITOS PubSub events

Status: Accepted

## Context

External subscribers need system and usage changes without coupling to internal
cleanup events such as `EntityBeingDeletedEvent<T>`. Domain-event names describe
business actions; PubSub topics and payloads are separate public contracts.

## Decision

Application-service handlers map domain events to `IEventBody` models and publish
through `IKitosEventPublisherService` to PubSub's `api/v1/publish` endpoint.
`KitosQueueTopics` defines the current topics:

| Topic | Trigger | Payload |
|---|---|---|
| `KitosITSystemChangedEvent` | System name changes, or a DPR's data-processor set changes (one event per associated usage). Requires a before-change snapshot. | `SystemUuid`, plus changed fields only: `SystemName` for system renames; `DataProcessorUuid` and `DataProcessorName` for processor changes. Processor selection uses the last DPR processor, falling back to the system's rights holder; absent values can be null. |
| `KitosItSystemUsageCreatedEvent` | `SystemTakenIntoUsageEvent`, raised after the creation write service commits its transaction scope. | Exactly `SystemUuid` and `OrganizationUuid`. |
| `KitosItSystemUsageDeletedEvent` | `SystemTakenOutOfUsageEvent`, raised after the outermost transaction commits. Includes direct deletion, archiving, and cascading system/organization deletion. | Exactly `SystemUuid` and `OrganizationUuid`. |

Usage payload UUIDs identify the system and the organization using it, not the
usage itself. Preserve the public "Deleted" topic/body/handler names even though
the domain event is named `SystemTakenOutOfUsageEvent`.

For deletion, capture UUIDs before removing the entity and register an
`IDatabaseTransaction.AfterCommit` callback in the shared usage deletion service.
Committed nested scopes transfer callbacks to their parent; rollback or disposal
without commit discards them. Callbacks require transactions owned by
`ITransactionManager`. Existing pre-deletion cleanup events remain unchanged.
Creation and system-change events retain their existing timing; they do not
currently use this outermost-commit callback mechanism.

## Consequences

Publication is best-effort. Returned errors, unsuccessful HTTP responses, and
exceptions are logged without undoing committed deletions. There is no durable
outbox or retry; a process crash between commit and publication can lose an event.
Subscribers explicitly subscribe to each topic. Topic names, payload keys, and
the distinction between omitted and null fields must remain compatible.
