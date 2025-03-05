using Core.DomainModel.Events;
using Core.DomainModel.ItSystem;

namespace Core.ApplicationServices.Model.EventHandler;

public class PublishSystemChangesEventHandler : IDomainEventHandler<EntityUpdatedEvent<ItSystem>>
{
    private readonly IKitosEventPublisherService _eventPublisher;
    private const string SystemQueueTopic = "some-topic";

    public PublishSystemChangesEventHandler(IKitosEventPublisherService eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
    public void Handle(EntityUpdatedEvent<ItSystem> domainEvent)
    {
        var system = domainEvent.Entity;
        var eventBody = new SystemChangeEventModel() {SystemName = system.Name, SystemUuid = system.Uuid};
        var newEvent = new KitosEvent(eventBody, SystemQueueTopic);
        _eventPublisher.PublishEvent(newEvent);
    }
}