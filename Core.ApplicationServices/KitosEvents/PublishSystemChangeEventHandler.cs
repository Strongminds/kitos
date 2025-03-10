using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Extensions;
using Core.Abstractions.Types;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.KitosEvents;
using Core.DomainModel.Events;
using Core.DomainModel.GDPR.Events;
using Core.DomainModel.ItSystem.DomainEvents;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;

namespace Core.ApplicationServices.KitosEvents;

public class PublishSystemChangesEventHandler : IDomainEventHandler<ItSystemChangedEvent>, IDomainEventHandler<DprChangedEvent>
{
    private readonly IKitosEventPublisherService _eventPublisher;
    private const string SystemQueueTopic = "KitosITSystemChangedEvent";

    public PublishSystemChangesEventHandler(IKitosEventPublisherService eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
    public void Handle(ItSystemChangedEvent domainEvent)
    {
        var changeEvent = CalculateChangeEventFromSystemModel(domainEvent);
        if (changeEvent.IsNone)
        {
            return;
        }
        var newEvent = new KitosEvent(changeEvent.Value, SystemQueueTopic);
        _eventPublisher.PublishEvent(newEvent);
    }

    public void Handle(DprChangedEvent domainEvent)
    {
        var changeEvents = CalculateChangeEventsFromDprModel(domainEvent);
        if (changeEvents.IsNone)
        {
            return;
        }

        foreach (var changeEvent in changeEvents.Value)
        {
            var newEvent = new KitosEvent(changeEvent, SystemQueueTopic);
            _eventPublisher.PublishEvent(newEvent);
        }
    }

    private static Maybe<IEnumerable<SystemChangeEventModel>> CalculateChangeEventsFromDprModel(DprChangedEvent domainEvent)
    {
        var snapshot = domainEvent.Snapshot;
        var dprAfter = domainEvent.Entity;
        if (snapshot == null)
        {
            return Maybe<IEnumerable<SystemChangeEventModel>>.None;
        }

        var dataProcessorUuidsAfter = dprAfter.DataProcessors.Select(x => x.Uuid).ToHashSet();

        if (snapshot.DataProcessorUuids.SetEquals(dataProcessorUuidsAfter))
        {
            return Maybe<IEnumerable<SystemChangeEventModel>>.None;
        }

        var dataProcessor = dprAfter.DataProcessors.Last().FromNullable();
        return dprAfter.SystemUsages?.Select(usage => GetEventFromUsage(usage, dataProcessor)).FromNullable();
    }

    private static SystemChangeEventModel GetEventFromUsage(ItSystemUsage usage, Maybe<Organization> dataProcessor)
    {
        return new SystemChangeEventModel
        {
            SystemUuid = usage.ItSystem.Uuid,
            DataProcessorUuid = dataProcessor.Select(x => x.Uuid).AsChangedValue(),
            DataProcessorName = dataProcessor.Select(x => x.Name).GetValueOrDefault().AsChangedValue()
        };
    }

    private static Maybe<SystemChangeEventModel> CalculateChangeEventFromSystemModel(ItSystemChangedEvent changeEvent)
    {
        var snapshot = changeEvent.Snapshot;
        var systemAfter = changeEvent.Entity;
        if (snapshot == null)
        {
            return Maybe<SystemChangeEventModel>.None;
        }

        if (snapshot.Name.Equals(systemAfter.Name))
        {
            return Maybe<SystemChangeEventModel>.None;
        }

        return new SystemChangeEventModel
        {
            SystemUuid = systemAfter.Uuid,
            SystemName = systemAfter.Name.AsChangedValue()
        };
    }
}