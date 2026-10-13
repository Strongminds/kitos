using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Abstractions.Types;
using Core.ApplicationServices.Users;
using Core.DomainModel.Organization;
using Core.DomainModel.SSO;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Repositories.SSO;
using Core.DomainServices.Users;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Presentation.Web.Infrastructure.UserSync;
using UserSync.Transport;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure.UserSync;

public class KitosUserChangeDeliveryTest
{
    [Theory]
    [InlineData("accepted", UserChangeDeliveryResult.Accepted)]
    [InlineData("unknown", UserChangeDeliveryResult.Rejected)]
    [InlineData("disconnected", UserChangeDeliveryResult.Rejected)]
    [InlineData("conflict", UserChangeDeliveryResult.Rejected)]
    public async Task Deliver_UsesFreshScopeAndMapsApplicationResult(string scenario, UserChangeDeliveryResult expected)
    {
        var instances = new List<ScopeMarker>();
        var message = new UserDeletionEvent("id", Guid.NewGuid(), Guid.NewGuid(), OccurredAt: DateTime.UtcNow);
        var organization = new Organization { Id = 12, Uuid = message.OrganizationUuid, FkOrgUsersConnected = scenario != "disconnected" };
        var organizations = new Mock<IOrganizationRepository>();
        organizations.Setup(x => x.GetByUuid(message.OrganizationUuid))
            .Returns(scenario == "unknown" ? Maybe<Organization>.None : organization);
        var identities = new Mock<ISsoUserIdentityRepository>();
        identities.Setup(x => x.GetByExternalUuid(message.ExternalUserUuid)).Returns(Maybe<SsoUserIdentity>.None);
        var repository = new Mock<IExternalUserChangeRepository>();
        repository.Setup(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()))
            .Returns((ExternalUserChange change) => change);
        if (scenario == "conflict")
            repository.Setup(x => x.FindByMessageId("id")).Returns(new ExternalUserChange { OrganizationId = 99 });
        var services = new ServiceCollection();
        services.AddScoped(_ => { var marker = new ScopeMarker(); instances.Add(marker); return marker; });
        services.AddScoped(sp =>
        {
            sp.GetRequiredService<ScopeMarker>();
            return new ExternalUserChangeIngestionService(repository.Object, organizations.Object, identities.Object);
        });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var delivery = new KitosUserChangeDelivery(provider.GetRequiredService<IServiceScopeFactory>());
        Assert.Equal(expected, await delivery.Deliver(message, default));
        Assert.Equal(expected, await delivery.Deliver(message, default));
        Assert.Equal(2, instances.Count);
        Assert.All(instances, x => Assert.True(x.Disposed));
        if (scenario == "accepted")
            repository.Verify(x => x.InsertAndSave(It.Is<ExternalUserChange>(change =>
                change.ExternalMessageId == message.ExternalMessageId && change.ExternalUserUuid == message.ExternalUserUuid &&
                change.OrganizationId == organization.Id && change.Status == ExternalUserChangeStatus.Pending)), Times.Exactly(2));
    }

    private sealed class ScopeMarker : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
}
