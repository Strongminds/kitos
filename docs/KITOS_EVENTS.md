# KITOS usage deletion event

KITOS publishes usage deletions to the dedicated `KitosItSystemUsageDeletedEvent`
topic, separate from `KitosItSystemUsageCreatedEvent`.

The payload contains exactly these fields:

```json
{
  "SystemUuid": "22222222-2222-2222-2222-222222222222",
  "OrganizationUuid": "33333333-3333-3333-3333-333333333333"
}
```

The UUIDs identify the system and organization of the deleted usage. All usage
deletions are covered, including archiving and cascading system or organization
deletion. Existing pre-deletion cleanup and lifecycle tracking are unchanged.

One publication attempt is made per deleted usage, only after the outermost
database transaction commits. Failed or rolled-back deletions publish nothing.
Nested transaction scopes pass callbacks to their parent when committed and
discard them when rolled back or disposed without committing. Callbacks require
transactions owned by `ITransactionManager`.

Delivery is best-effort: PubSub errors are logged without undoing committed
deletions. There is no durable outbox or retry; a process crash between commit and
publication can lose an event. Subscribers must subscribe to the deletion topic
explicitly.
