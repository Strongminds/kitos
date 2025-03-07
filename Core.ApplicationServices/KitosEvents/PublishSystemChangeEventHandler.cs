using Core.ApplicationServices.Model.KitosEvents;
using Core.DomainModel.Events;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystem.DomainEvents;

namespace Core.ApplicationServices.KitosEvents;

public class PublishSystemChangesEventHandler : IDomainEventHandler<ItSystemChangedEvent>
{
    private readonly IKitosEventPublisherService _eventPublisher;
    private const string SystemQueueTopic = "KitosITSystemChangedEvent";

    public PublishSystemChangesEventHandler(IKitosEventPublisherService eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
    public void Handle(ItSystemChangedEvent domainEvent)
    {
        var system = domainEvent.Entity;
        var snapshot = domainEvent.Snapshot;
        var eventBody = new SystemChangeEventModel() { SystemName = system.Name, SystemUuid = system.Uuid };
        var newEvent = new KitosEvent(eventBody, SystemQueueTopic);
        _eventPublisher.PublishEvent(newEvent);
    }
}