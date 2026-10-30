using System;
using System.Linq;
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
        _user.OrganizationRights.Add(new OrganizationRight { OrganizationId = _organization.Id, Role = OrganizationRole.User });
        _identities.Setup(x => x.GetByExternalUuid(_input.ExternalUserUuid)).Returns(new SsoUserIdentity(_input.ExternalUserUuid, _user));
        _repository.Setup(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()))
            .Returns((ExternalUserChange change) => change);
        _repository.Setup(x => x.AsQueryable()).Returns(Array.Empty<ExternalUserChange>().AsQueryable());
        var transaction = new Mock<IExternalUserChangeIngestionTransaction>();
        transaction.Setup(x => x.Execute(It.IsAny<Func<Result<ExternalUserChange, OperationError>>>()))
            .Returns((Func<Result<ExternalUserChange, OperationError>> action) => action());
        _sut = new(_repository.Object, _organizations.Object, _identities.Object, transaction.Object);
    }

    [Fact]
    public void MatchingBindingCreatesPendingWithoutChangingUser()
    {
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
    public void Ingest_DifferentMessageForPendingUser_ReturnsConflict(bool differentExternalIdentity)
    {
        var pending = Existing();
        pending.ExternalMessageId = "earlier-message";
        pending.UserId = _user.Id;
        if (differentExternalIdentity) pending.ExternalUserUuid = Guid.NewGuid();
        _repository.Setup(x => x.AsQueryable()).Returns(new[] { pending }.AsQueryable());

        var result = _sut.Ingest(_input);

        Assert.Equal(OperationFailure.Conflict, result.Error.FailureType);
        Assert.Equal(ExternalUserChangeStatus.Pending, pending.Status);
        _repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ingest_PendingChangeForOtherUserOrOrganization_AllowsNewChange(bool otherOrganization)
    {
        var pending = Existing();
        pending.UserId = _user.Id;
        if (otherOrganization) pending.OrganizationId++;
        else
        {
            pending.UserId++;
            pending.ExternalUserUuid = Guid.NewGuid();
        }
        _repository.Setup(x => x.AsQueryable()).Returns(new[] { pending }.AsQueryable());

        Assert.True(_sut.Ingest(_input).Ok);
        _repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Once);
    }

    [Theory]
    [InlineData(ExternalUserChangeStatus.Dismissed)]
    [InlineData(ExternalUserChangeStatus.Applied)]
    public void Ingest_NewMessageAfterResolution_RequiresActiveMember(ExternalUserChangeStatus status)
    {
        var previous = Existing();
        previous.Status = status;
        _repository.Setup(x => x.AsQueryable()).Returns(new[] { previous }.AsQueryable());
        _repository.Setup(x => x.FindByMessageId(previous.ExternalMessageId)).Returns(previous);
        var newEvent = _input with { ExternalMessageId = "new-message" };
        if (status == ExternalUserChangeStatus.Applied)
        {
            _user.Deleted = true;
            Assert.Equal(OperationFailure.NotFound, _sut.Ingest(newEvent).Error.FailureType);
            _repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Never);
            _user.Deleted = false;
            _user.OrganizationRights.Clear();
            Assert.Equal(OperationFailure.NotFound, _sut.Ingest(newEvent).Error.FailureType);
            _user.OrganizationRights.Add(new OrganizationRight { OrganizationId = _organization.Id, Role = OrganizationRole.User });
        }

        var result = _sut.Ingest(newEvent);

        Assert.True(result.Ok);
        Assert.Equal(ExternalUserChangeStatus.Pending, result.Value.Status);
        Assert.Equal(status, previous.Status);
        Assert.NotEqual(previous.Uuid, result.Value.Uuid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ingest_MissingOrWrongOrganizationBinding_DoesNotSaveChange(bool hasIdentity)
    {
        _user.OrganizationRights.Clear();
        _identities.Setup(x => x.GetByExternalUuid(_input.ExternalUserUuid)).Returns(Maybe<SsoUserIdentity>.None);
        if (hasIdentity)
        {
            _user.OrganizationRights.Add(new OrganizationRight { OrganizationId = _organization.Id + 1 });
            _identities.Setup(x => x.GetByExternalUuid(_input.ExternalUserUuid)).Returns(new SsoUserIdentity(_input.ExternalUserUuid, _user));
        }
        var result = _sut.Ingest(_input);
        Assert.Equal(OperationFailure.NotFound, result.Error.FailureType);
        _repository.Verify(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()), Times.Never);
    }

    [Fact]
    public void DuplicateDoesNotInsertOrResetResolvedStatus()
    {
        var existing = Existing();
        existing.Status = ExternalUserChangeStatus.Dismissed;
        _repository.Setup(x => x.FindByMessageId(_input.ExternalMessageId)).Returns(existing);
        _identities.Setup(x => x.GetByExternalUuid(_input.ExternalUserUuid)).Returns(Maybe<SsoUserIdentity>.None);
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
