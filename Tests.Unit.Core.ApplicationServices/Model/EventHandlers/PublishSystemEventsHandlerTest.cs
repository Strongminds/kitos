﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Extensions;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.KitosEvents;
using Core.ApplicationServices.Model.KitosEvents;
using Core.DomainModel.GDPR;
using Core.DomainModel.GDPR.Events;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystem.DomainEvents;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.Model.EventHandlers;

public class PublishSystemEventsHandlerTest : WithAutoFixture
{
    private readonly PublishSystemChangesEventHandler _sut;
    private readonly Mock<IKitosEventPublisherService> _eventPublisher;

    private const string ExpectedQueueTopic = "KitosITSystemChangedEvent";

    public PublishSystemEventsHandlerTest()
    {
        _eventPublisher = new Mock<IKitosEventPublisherService>();
        _sut = new PublishSystemChangesEventHandler(_eventPublisher.Object);
    }

    [Fact]
    public void Can_Publish_System_Name_Change()
    {
        var snapshot = A<SystemSnapshot>();
        var itSystem = CreateItSystem();
        var newEvent = new ItSystemChangedEvent(itSystem, snapshot);
        var expectedBody = new SystemChangeEventModel
        {
            SystemUuid = itSystem.Uuid,
            SystemName = itSystem.Name.AsChangedValue(),
        };
        var expectedEvent = new KitosEvent(expectedBody, ExpectedQueueTopic);

        _sut.Handle(newEvent);

        VerifyEventIsPublished(expectedEvent);
    }

    [Fact]
    public void Can_Publish_Data_Processor_Change()
    {
        var snapshot = A<DprSnapshot>();
        var dpr = CreateDpr(snapshot.DataProcessorUuids);
        var newestProcessor = new Organization { Uuid = A<Guid>(), Name = A<string>() };
        dpr.DataProcessors.Add(newestProcessor);

        var newEvent = new DprChangedEvent(dpr, snapshot);
        var expectedBody = new SystemChangeEventModel
        {
            SystemUuid = dpr.SystemUsages.First().ItSystem.Uuid,
            DataProcessorName = newestProcessor.Name.AsChangedValue(),
            DataProcessorUuid = newestProcessor.Uuid.FromNullable().AsChangedValue()
        };
        var expectedEvent = new KitosEvent(expectedBody, ExpectedQueueTopic);

        _sut.Handle(newEvent);

        VerifyEventIsPublished(expectedEvent);
    }

    private void VerifyEventIsPublished(KitosEvent expectedEvent)
    {
        _eventPublisher.Verify(x => x.PublishEvent(It.Is<KitosEvent>(e => EventsMatch(e, expectedEvent)
        )), Times.Once);
    }

    private bool EventsMatch(KitosEvent event1, KitosEvent event2)
    {
        var kvp1 = event1.EventBody.ToKeyValuePairs();
        var kvp2 = event2.EventBody.ToKeyValuePairs();
        return event1.Topic == event2.Topic && DictionariesAreEqual(kvp1, kvp2);
    }

    private bool DictionariesAreEqual(Dictionary<string, object> dict1, Dictionary<string, object> dict2)
    {
        if (dict1.Count != dict2.Count)
            return false;
        foreach (var kvp in dict1)
        {
            if (!dict2.TryGetValue(kvp.Key, out var value))
                return false;
            if (!Equals(kvp.Value, value))
                return false;
        }
        return true;
    }

    private ItSystem CreateItSystem()
    {
        return new ItSystem
        {
            Uuid = A<Guid>(),
            Name = A<string>(),
            BelongsTo = new Organization { Uuid = A<Guid>(), Name = A<string>() }
        };
    }

    private DataProcessingRegistration CreateDpr(IEnumerable<Guid> uuids)
    {
        return new DataProcessingRegistration { SystemUsages = new List<ItSystemUsage>{new ItSystemUsage {ItSystem = CreateItSystem()}}, DataProcessors = uuids.Select(x => new Organization { Uuid = x }).ToList() };
    }

}