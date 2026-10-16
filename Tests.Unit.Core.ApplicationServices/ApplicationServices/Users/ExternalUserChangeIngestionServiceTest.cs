using System;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Users;
using Core.ApplicationServices.Users;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainModel.SSO;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Repositories.SSO;
using Core.DomainServices.Users;
using Moq;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.ApplicationServices.Users;

public class ExternalUserChangeIngestionServiceTest
{
    private readonly Mock<IExternalUserChangeRepository> _repository = new();
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<ISsoUserIdentityRepository> _identities = new();
    private readonly Organization _organization = new() { Id = 12, Uuid = Guid.NewGuid(), FkOrgUsersConnected = true };
    private readonly User _user = new() { Id = 42 };
    private readonly ExternalUserChangeInput _input;
    private readonly ExternalUserChangeIngestionService _sut;

    public ExternalUserChangeIngestionServiceTest()
    {
        _input = new("message-1", _organization.Uuid, Guid.NewGuid(), ExternalUserChangeType.Deleted, DateTime.UtcNow);
        _organizations.Setup(x => x.GetByUuid(_organization.Uuid)).Returns(_organization);
        _identities.Setup(x => x.GetByExternalUuid(It.IsAny<Guid>())).Returns(Maybe<SsoUserIdentity>.None);
        _repository.Setup(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()))
            .Returns((ExternalUserChange change) => change);
        _sut = new(_repository.Object, _organizations.Object, _identities.Object);
    }

    [Fact]
    public void MatchingBindingCreatesPendingWithoutChangingUser()
    {
        _user.OrganizationRights.Add(new OrganizationRight { OrganizationId = _organization.Id, Role = OrganizationRole.User });
        _identities.Setup(x => x.GetByExternalUuid(_input.ExternalUserUuid)).Returns(new SsoUserIdentity(_input.ExternalUserUuid, _user));
        var result = _sut.Ingest(_input);
        Assert.True(result.Ok);
        Assert.Equal(_user.Id, result.Value.UserId);
        Assert.Equal(ExternalUserChangeStatus.Pending, result.Value.Status);
        Assert.False(_user.Deleted);
        Assert.Single(_user.OrganizationRights);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrWrongOrganizationBindingIsRetainedUnmatched(bool hasIdentity)
    {
        if (hasIdentity)
        {
            _user.OrganizationRights.Add(new OrganizationRight { OrganizationId = _organization.Id + 1 });
            _identities.Setup(x => x.GetByExternalUuid(_input.ExternalUserUuid)).Returns(new SsoUserIdentity(_input.ExternalUserUuid, _user));
        }
        var result = _sut.Ingest(_input);
        Assert.True(result.Ok);
        Assert.Null(result.Value.UserId);
        Assert.Equal(ExternalUserChangeStatus.Pending, result.Value.Status);
    }

    [Fact]
    public void DuplicateDoesNotInsertOrResetResolvedStatus()
    {
        var existing = Existing();
        existing.Status = ExternalUserChangeStatus.Dismissed;
        _repository.Setup(x => x.FindByMessageId(_input.ExternalMessageId)).Returns(existing);
        var result = _sut.Ingest(_input);
        Assert.Same(existing, result.Value);
        Assert.Equal(ExternalUserChangeStatus.Dismissed, result.Value.Status);
        _repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Never);
    }

    [Fact]
    public void ConcurrentInsertReturnsThePersistedRecord()
    {
        var existing = Existing();
        _repository.Setup(x => x.InsertAndSave(It.IsAny<ExternalUserChange>())).Returns(existing);
        Assert.Same(existing, (_sut.Ingest(_input)).Value);
    }

    [Fact]
    public void ReusedMessageIdForDifferentUserIsConflict()
    {
        _repository.Setup(x => x.FindByMessageId(_input.ExternalMessageId)).Returns(Existing());
        var result = _sut.Ingest(_input with { ExternalUserUuid = Guid.NewGuid() });
        Assert.Equal(OperationFailure.Conflict, result.Error.FailureType);
    }

    [Fact]
    public void DuplicateSurvivesPostgreSqlTimestampPrecision()
    {
        var occurredAt = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc).AddTicks(17);
        var existing = Existing();
        existing.OccurredAt = occurredAt.AddTicks(-7);
        _repository.Setup(x => x.FindByMessageId(_input.ExternalMessageId)).Returns(existing);
        Assert.True((_sut.Ingest(_input with { OccurredAt = occurredAt })).Ok);
    }

    [Fact]
    public void OrganizationWithoutUsersConnectionIsRejectedAsConflict()
    {
        _organization.FkOrgUsersConnected = false;
        var result = _sut.Ingest(_input);
        Assert.Equal(OperationFailure.Conflict, result.Error.FailureType);
        _repository.Verify(x => x.FindByMessageId(It.IsAny<string>()), Times.Never);
        _repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Never);
    }

    [Fact]
    public void UnsupportedTypeIsRejected()
    {
        var result = _sut.Ingest(_input with { ChangeType = (ExternalUserChangeType)99 });
        Assert.Equal(OperationFailure.BadInput, result.Error.FailureType);
        _repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Never);
    }

    private ExternalUserChange Existing() => new()
    {
        ExternalMessageId = _input.ExternalMessageId,
        OrganizationId = _organization.Id,
        ExternalUserUuid = _input.ExternalUserUuid,
        ChangeType = _input.ChangeType,
        OccurredAt = _input.OccurredAt
    };
}
