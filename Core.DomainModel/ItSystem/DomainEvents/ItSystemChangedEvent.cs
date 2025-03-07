using Core.DomainModel.Events;

namespace Core.DomainModel.ItSystem.DomainEvents
{
    public class ItSystemChangedEvent : EntityUpdatedEvent<ItSystem>
    {
        public ItSystemChangedEvent(ItSystem entity, SystemSnapshot snapshot) : base(entity)
        {
            Snapshot = snapshot;
        }

        public SystemSnapshot Snapshot { get; }
    }
}
