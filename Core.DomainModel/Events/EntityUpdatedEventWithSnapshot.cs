using Core.Abstractions.Types;

namespace Core.DomainModel.Events;

public class EntityUpdatedEventWithSnapshot<TEntity, TSnapshot> : EntityUpdatedEvent<TEntity>
{
    public EntityUpdatedEventWithSnapshot(TEntity entity, Maybe<TSnapshot> snapshot) : base(entity)
    {
        Snapshot = snapshot;
    }

    public Maybe<TSnapshot> Snapshot { get; set; }
}