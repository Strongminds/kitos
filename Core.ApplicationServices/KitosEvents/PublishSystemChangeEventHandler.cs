using Core.Abstractions.Types;
using Core.ApplicationServices.Model.KitosEvents;
using Core.DomainModel.Events;
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
        var changeEvent = CalculateChangeEventModel(domainEvent);
        if (changeEvent.IsNone)
        {
            return;
        }
        var newEvent = new KitosEvent(changeEvent.Value, SystemQueueTopic);
        _eventPublisher.PublishEvent(newEvent);
    }

    private static Maybe<SystemChangeEventModel> CalculateChangeEventModel(ItSystemChangedEvent changeEvent)
    {
        var snapshot = changeEvent.Snapshot;
        var systemAfter = changeEvent.Entity;

        if (snapshot == null)
        {
            return Maybe<SystemChangeEventModel>.None;
        }

        var changeModel = new SystemChangeEventModel();
        bool hasChanges = false;

        if (snapshot.Name != systemAfter.Name)
        {
            changeModel.SystemName = systemAfter.Name;
            hasChanges = true;
        }
        
        return hasChanges
            ? Maybe<SystemChangeEventModel>.Some(changeModel)
            : Maybe<SystemChangeEventModel>.None;
    }

}