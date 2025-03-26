using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Extensions;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.KitosEvents;
using Core.ApplicationServices.Model.KitosEvents;
using Core.DomainModel.Events;
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
        var snapshot = A<ItSystemSnapshot>();
        var itSystem = CreateItSystem();
        var newEvent = new EntityUpdatedEventWithSnapshot<ItSystem, ItSystemSnapshot>(itSystem, snapshot);
        var expectedBody = new SystemNameChangeEventBodyModel
        {
            SystemUuid = itSystem.Uuid,
            SystemName = itSystem.Name,
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

        var newEvent = new EntityUpdatedEventWithSnapshot<DataProcessingRegistration, DprSnapshot>(dpr, snapshot);
        var expectedBody = new SystemDataProcessorChangeEventBodyModel
        {
            SystemUuid = dpr.SystemUsages.First().ItSystem.Uuid,
            DataProcessorName = newestProcessor.Name,
            DataProcessorUuid = newestProcessor.Uuid
        };
        var expectedEvent = new KitosEvent(expectedBody, ExpectedQueueTopic);

        _sut.Handle(newEvent);

        VerifyEventIsPublished(expectedEvent);
    }

    [Fact]
    public void Publishes_RightsHolder_If_No_Data_Processors()
    {
        var snapshot = A<DprSnapshot>();
        var dpr = CreateDpr(new List<Guid>());
        var system = dpr.SystemUsages.First().ItSystem;
        var newEvent = new EntityUpdatedEventWithSnapshot<DataProcessingRegistration, DprSnapshot>(dpr, snapshot);
        var expectedBody = new SystemDataProcessorChangeEventBodyModel
        {
            SystemUuid = system.Uuid,
            DataProcessorUuid = system.GetRightsHolder().Select(x => x.Uuid).GetValueOrNull(),
            DataProcessorName = system.GetRightsHolder().Select(x => x.Name).GetValueOrDefault()
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

    private static bool EventsMatch(KitosEvent event1, KitosEvent event2)
    {
        Assert.Equivalent(event1, event2);
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
        return new DataProcessingRegistration { SystemUsages = new List<ItSystemUsage> { new ItSystemUsage { ItSystem = CreateItSystem() } }, DataProcessors = uuids.Select(x => new Organization { Uuid = x }).ToList() };
    }

}