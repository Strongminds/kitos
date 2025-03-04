using Core.DomainModel.Events;
using Core.DomainModel.ItSystem;

namespace Core.ApplicationServices.Model.EventHandler;

public class PublishSystemChangesEventHandler : IDomainEventHandler<EntityUpdatedEvent<ItSystem>>
{
    private readonly IKitosEventPublisherService _eventPublisher;

    public PublishSystemChangesEventHandler(IKitosEventPublisherService eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
    public void Handle(EntityUpdatedEvent<ItSystem> domainEvent)
    {
        var system = domainEvent.Entity;
        var eventBody = new SystemChangeEvent() {SystemName = system.Name, SystemUuid = system.Uuid};
        var newEvent = new KitosEvent(eventBody, "some-topic");
        _eventPublisher.PublishEvent(newEvent);
    }
}