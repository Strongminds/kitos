using Core.DomainModel.Events;
using Core.DomainModel.ItSystem;

namespace Core.DomainModel.GDPR.Events
{
    public class DprChangedEvent : EntityUpdatedEvent<DataProcessingRegistration>
    {
        public DprChangedEvent(DataProcessingRegistration entity, DprSnapshot snapshot) : base(entity)
        {
            Snapshot = snapshot;
        }

        public  DprSnapshot Snapshot { get; }
    }
}
