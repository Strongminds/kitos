using System;
using Core.ApplicationServices.KitosEvents;
using Core.ApplicationServices.Model.KitosEvents;
using Core.DomainModel.Events;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.Model.EventHandlers;

public class PublishSystemTakenIntoUsageEventHandlerTest : WithAutoFixture
{
    private readonly Mock<IKitosEventPublisherService> _eventPublisher;
    private readonly PublishSystemTakenIntoUsageEventHandler _sut;

    public PublishSystemTakenIntoUsageEventHandlerTest()
    {
        _eventPublisher = new Mock<IKitosEventPublisherService>();
        _sut = new PublishSystemTakenIntoUsageEventHandler(_eventPublisher.Object);
    }

    [Fact]
    public void Can_Publish_System_Taken_Into_Usage()
    {
        var systemUuid = A<Guid>();
        var organizationUuid = A<Guid>();
        var systemUsage = new ItSystemUsage
        {
            ItSystem = new ItSystem { Uuid = systemUuid },
            Organization = new Organization { Uuid = organizationUuid }
        };

        _sut.Handle(new EntityCreatedEvent<ItSystemUsage>(systemUsage));

        _eventPublisher.Verify(x => x.PublishEvent(It.Is<KitosEvent>(e =>
            e.Topic == KitosQueueTopics.SystemTakenIntoUsageEventTopic &&
            ((SystemTakenIntoUsageEventBodyModel)e.EventBody).SystemUuid == systemUuid &&
            ((SystemTakenIntoUsageEventBodyModel)e.EventBody).OrganizationUuid == organizationUuid)), Times.Once);
    }
}
