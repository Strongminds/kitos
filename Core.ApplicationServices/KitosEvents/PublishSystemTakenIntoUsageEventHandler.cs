using Core.ApplicationServices.Model.KitosEvents;
using Core.DomainModel.Events;
using Core.DomainModel.ItSystemUsage;

namespace Core.ApplicationServices.KitosEvents;

public class PublishSystemTakenIntoUsageEventHandler(IKitosEventPublisherService eventPublisher)
    : IDomainEventHandler<EntityCreatedEvent<ItSystemUsage>>
{
    private const string QueueTopic = KitosQueueTopics.SystemTakenIntoUsageEventTopic;

    public void Handle(EntityCreatedEvent<ItSystemUsage> domainEvent)
    {
        var eventBody = new SystemTakenIntoUsageEventBodyModel{ SystemUuid = domainEvent.Entity.ItSystem.Uuid, OrganizationUuid = domainEvent.Entity.Organization.Uuid };

        var newEvent = new KitosEvent(eventBody, QueueTopic);
        eventPublisher.PublishEvent(newEvent);
    }
}
