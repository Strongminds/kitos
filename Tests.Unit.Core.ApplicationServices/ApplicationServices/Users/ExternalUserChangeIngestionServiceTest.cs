using System;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly Mock<IExternalUserChangeStore> _store = new();
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<ISsoUserIdentityRepository> _identities = new();
    private readonly Organization _organization = new() { Id = 12, Uuid = Guid.NewGuid() };
    private readonly User _user = new() { Id = 42 };
    private readonly ExternalUserChangeInput _input;
    private readonly ExternalUserChangeIngestionService _sut;

    public ExternalUserChangeIngestionServiceTest()
    {
        _input = new("message-1", _organization.Uuid, Guid.NewGuid(), ExternalUserChangeType.Deleted, DateTime.UtcNow);
        _organizations.Setup(x => x.GetByUuid(_organization.Uuid)).Returns(_organization);
        _identities.Setup(x => x.GetByExternalUuid(It.IsAny<Guid>())).Returns(Maybe<SsoUserIdentity>.None);
        _store.Setup(x => x.Insert(It.IsAny<ExternalUserChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalUserChange change, CancellationToken _) => change);
        _sut = new(_store.Object, _organizations.Object, _identities.Object);
    }

    [Fact]
    public async Task MatchingBindingCreatesPendingWithoutChangingUser()
    {
        _user.OrganizationRights.Add(new OrganizationRight { OrganizationId = _organization.Id, Role = OrganizationRole.User });
        _identities.Setup(x => x.GetByExternalUuid(_input.ExternalUserUuid)).Returns(new SsoUserIdentity(_input.ExternalUserUuid, _user));
        var result = await _sut.Ingest(_input);
        Assert.True(result.Ok);
        Assert.Equal(_user.Id, result.Value.UserId);
        Assert.Equal(ExternalUserChangeStatus.Pending, result.Value.Status);
        Assert.False(_user.Deleted);
        Assert.Single(_user.OrganizationRights);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrWrongOrganizationBindingIsRetainedUnmatched(bool hasIdentity)
    {
        if (hasIdentity)
        {
            _user.OrganizationRights.Add(new OrganizationRight { OrganizationId = _organization.Id + 1 });
            _identities.Setup(x => x.GetByExternalUuid(_input.ExternalUserUuid)).Returns(new SsoUserIdentity(_input.ExternalUserUuid, _user));
        }
        var result = await _sut.Ingest(_input);
        Assert.True(result.Ok);
        Assert.Null(result.Value.UserId);
        Assert.Equal(ExternalUserChangeStatus.Pending, result.Value.Status);
    }

    [Fact]
    public async Task DuplicateDoesNotInsertOrResetResolvedStatus()
    {
        var existing = Existing();
        existing.Status = ExternalUserChangeStatus.Dismissed;
        _store.Setup(x => x.FindByMessageId(_input.ExternalMessageId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var result = await _sut.Ingest(_input);
        Assert.Same(existing, result.Value);
        Assert.Equal(ExternalUserChangeStatus.Dismissed, result.Value.Status);
        _store.Verify(x => x.Insert(It.IsAny<ExternalUserChange>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConcurrentInsertReturnsThePersistedRecord()
    {
        var existing = Existing();
        _store.Setup(x => x.Insert(It.IsAny<ExternalUserChange>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        Assert.Same(existing, (await _sut.Ingest(_input)).Value);
    }

    [Fact]
    public async Task ReusedMessageIdForDifferentUserIsConflict()
    {
        _store.Setup(x => x.FindByMessageId(_input.ExternalMessageId, It.IsAny<CancellationToken>())).ReturnsAsync(Existing());
        var result = await _sut.Ingest(_input with { ExternalUserUuid = Guid.NewGuid() });
        Assert.Equal(OperationFailure.Conflict, result.Error.FailureType);
    }

    [Fact]
    public async Task DuplicateSurvivesPostgreSqlTimestampPrecision()
    {
        var occurredAt = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc).AddTicks(17);
        var existing = Existing();
        existing.OccurredAt = occurredAt.AddTicks(-7);
        _store.Setup(x => x.FindByMessageId(_input.ExternalMessageId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        Assert.True((await _sut.Ingest(_input with { OccurredAt = occurredAt })).Ok);
    }

    [Fact]
    public async Task UnsupportedTypeIsRejected()
    {
        var result = await _sut.Ingest(_input with { ChangeType = (ExternalUserChangeType)99 });
        Assert.Equal(OperationFailure.BadInput, result.Error.FailureType);
        _store.Verify(x => x.Insert(It.IsAny<ExternalUserChange>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private ExternalUserChange Existing() => new()
    {
        ExternalMessageId = _input.ExternalMessageId, OrganizationId = _organization.Id,
        ExternalUserUuid = _input.ExternalUserUuid, ChangeType = _input.ChangeType, OccurredAt = _input.OccurredAt
    };
}
