using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Users;
using Core.DomainModel;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainModel.Organization.DomainEvents;
using Core.DomainModel.SSO;
using Core.DomainModel.Users;
using Core.DomainServices;
using Core.DomainServices.Repositories.SSO;
using Core.DomainServices.Users;
using Moq;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.ApplicationServices.Users;

public class ExternalUserChangeResolutionServiceTest
{
    private readonly Organization _organization = new() { Id = 10, Uuid = Guid.NewGuid() };
    private readonly User _user = new() { Id = 20 };
    private readonly ExternalUserChange _change;
    private readonly List<OrganizationRight> _rights;
    private readonly Mock<IGenericRepository<ExternalUserChange>> _changes = new();
    private readonly Mock<IGenericRepository<OrganizationRight>> _rightRepository = new();
    private readonly Mock<IOrganizationalUserContext> _actor = new();
    private readonly Mock<ISsoUserIdentityRepository> _identities = new();
    private readonly Mock<IDomainEvents> _events = new();
    private readonly Mock<IExternalUserChangeResolutionTransaction> _transaction = new();
    private readonly ExternalUserChangeResolutionService _sut;

    public ExternalUserChangeResolutionServiceTest()
    {
        _change = new ExternalUserChange
        {
            OrganizationId = _organization.Id,
            ExternalUserUuid = Guid.NewGuid(),
            ChangeType = ExternalUserChangeType.Deleted,
            Status = ExternalUserChangeStatus.Pending
        };
        _rights = [new OrganizationRight { UserId = _user.Id, OrganizationId = _organization.Id, Role = OrganizationRole.User }];
        var organizations = new Mock<IGenericRepository<Organization>>();
        organizations.Setup(x => x.AsQueryable()).Returns(new[] { _organization }.AsQueryable());
        _changes.Setup(x => x.AsQueryable()).Returns(new[] { _change }.AsQueryable());
        _rightRepository.Setup(x => x.AsQueryable()).Returns(() => _rights.AsQueryable());
        _rightRepository.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<OrganizationRight>>()))
            .Callback<IEnumerable<OrganizationRight>>(removed => _rights.RemoveAll(removed.ToList().Contains));
        _actor.Setup(x => x.UserId).Returns(30);
        _actor.Setup(x => x.HasRole(_organization.Id, OrganizationRole.LocalAdmin)).Returns(true);
        _identities.Setup(x => x.GetByExternalUuid(_change.ExternalUserUuid))
            .Returns(new SsoUserIdentity(_change.ExternalUserUuid, _user));
        _transaction.Setup(x => x.Execute(It.IsAny<Func<Result<ExternalUserChange, OperationError>>>()))
            .Returns((Func<Result<ExternalUserChange, OperationError>> action) => action());
        _sut = new(_transaction.Object, organizations.Object, _changes.Object, _rightRepository.Object,
            _actor.Object, _identities.Object, _events.Object);
    }

    [Fact]
    public void ResolveBatch_DeduplicatesAndResolvesEachItemSeparately()
    {
        var second = new ExternalUserChange { OrganizationId = _organization.Id, Status = ExternalUserChangeStatus.Pending };
        _changes.Setup(x => x.AsQueryable()).Returns(() => new[] { _change, second }.AsQueryable());

        var results = _sut.ResolveBatch(_organization.Uuid, [_change.Uuid, _change.Uuid, second.Uuid], false, default);

        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.True(result.Success));
        _transaction.Verify(x => x.ClearTracking(), Times.Exactly(2));
        _changes.Verify(x => x.Save(), Times.Exactly(2));
    }

    [Fact]
    public void Resolve_Apply_RemovesMembershipAndRaisesEvent()
    {
        var result = _sut.Resolve(_organization.Uuid, _change.Uuid, true);

        Assert.True(result.Ok);
        Assert.Empty(_rights);
        Assert.True(_user.Deleted);
        Assert.Equal(ExternalUserChangeStatus.Applied, _change.Status);
        Assert.Equal(30, _change.ResolvedByUserId);
        _changes.Verify(x => x.Save(), Times.Once);
        _events.Verify(x => x.Raise(It.Is<AdministrativeAccessRightsChanged>(e => e.UserId == _user.Id)), Times.Once);
    }

    [Fact]
    public void Resolve_Dismiss_PreservesMembershipAndDoesNotRaiseEvent()
    {
        var result = _sut.Resolve(_organization.Uuid, _change.Uuid, false);

        Assert.True(result.Ok);
        Assert.Single(_rights);
        Assert.Equal(ExternalUserChangeStatus.Dismissed, _change.Status);
        _events.Verify(x => x.Raise(It.IsAny<AdministrativeAccessRightsChanged>()), Times.Never);
    }

    [Fact]
    public void Resolve_ApplyWithOtherOrganization_PreservesUserAccess()
    {
        _rights.Add(new OrganizationRight { UserId = _user.Id, OrganizationId = 11, Role = OrganizationRole.User });

        var result = _sut.Resolve(_organization.Uuid, _change.Uuid, true);

        Assert.True(result.Ok);
        Assert.Single(_rights);
        Assert.Equal(11, _rights.Single().OrganizationId);
        Assert.False(_user.Deleted);
    }

    [Fact]
    public void Resolve_ChangedIdentity_DoesNotRemoveMembership()
    {
        _change.UserId = 99;

        var result = _sut.Resolve(_organization.Uuid, _change.Uuid, true);

        Assert.Equal(OperationFailure.Conflict, result.Error.FailureType);
        Assert.Single(_rights);
        Assert.Equal(ExternalUserChangeStatus.Pending, _change.Status);
        _changes.Verify(x => x.Save(), Times.Never);
    }

    [Fact]
    public void Resolve_MissingIdentity_DoesNotRemoveMembership()
    {
        _identities.Setup(x => x.GetByExternalUuid(_change.ExternalUserUuid)).Returns(Maybe<SsoUserIdentity>.None);

        var result = _sut.Resolve(_organization.Uuid, _change.Uuid, true);

        Assert.Equal(OperationFailure.Conflict, result.Error.FailureType);
        Assert.Single(_rights);
        _changes.Verify(x => x.Save(), Times.Never);
    }

    [Fact]
    public void Resolve_UnsupportedChangeType_DoesNotModifyChange()
    {
        _change.ChangeType = (ExternalUserChangeType)99;

        var result = _sut.Resolve(_organization.Uuid, _change.Uuid, true);

        Assert.Equal(OperationFailure.BadInput, result.Error.FailureType);
        Assert.Equal(ExternalUserChangeStatus.Pending, _change.Status);
        _changes.Verify(x => x.Save(), Times.Never);
    }

    [Fact]
    public void Resolve_NonAdministrator_DoesNotModifyChange()
    {
        _actor.Setup(x => x.HasRole(_organization.Id, OrganizationRole.LocalAdmin)).Returns(false);

        var result = _sut.Resolve(_organization.Uuid, _change.Uuid, true);

        Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        Assert.Equal(ExternalUserChangeStatus.Pending, _change.Status);
        _changes.Verify(x => x.Save(), Times.Never);
    }

    [Fact]
    public void Resolve_GlobalAdministrator_CannotBeRemoved()
    {
        _user.IsGlobalAdmin = true;

        var result = _sut.Resolve(_organization.Uuid, _change.Uuid, true);

        Assert.Equal(OperationFailure.Conflict, result.Error.FailureType);
        Assert.Single(_rights);
        _events.Verify(x => x.Raise(It.IsAny<AdministrativeAccessRightsChanged>()), Times.Never);
    }
}
