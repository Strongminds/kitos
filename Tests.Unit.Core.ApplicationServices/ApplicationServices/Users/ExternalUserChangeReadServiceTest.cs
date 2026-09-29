using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.Users;
using Core.ApplicationServices.Users;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Users;
using Moq;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.ApplicationServices.Users;

public class ExternalUserChangeReadServiceTest
{
    private readonly Organization _organization = new() { Id = 10, Uuid = Guid.NewGuid() };
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<IOrganizationalUserContext> _actor = new();
    private readonly Mock<IExternalUserChangeReadStore> _store = new();
    private readonly List<ExternalUserChange> _changes = [];
    private readonly ExternalUserChangeReadService _sut;

    public ExternalUserChangeReadServiceTest()
    {
        _organizations.Setup(x => x.GetByUuid(_organization.Uuid)).Returns(_organization);
        _actor.Setup(x => x.HasRole(_organization.Id, OrganizationRole.LocalAdmin)).Returns(true);
        _store.Setup(x => x.Query(_organization.Id)).Returns(() => _changes.AsQueryable());
        _store.Setup(x => x.Count(It.IsAny<IQueryable<ExternalUserChange>>()))
            .Returns((IQueryable<ExternalUserChange> query) => query.Count());
        _store.Setup(x => x.List(It.IsAny<IQueryable<ExternalUserChange>>()))
            .Returns((IQueryable<ExternalUserChange> query) => query.ToList());
        _sut = new(_organizations.Object, _actor.Object, _store.Object);
    }

    [Fact]
    public void List_FiltersSortsAndPages_WhileCountingAllPending()
    {
        _changes.AddRange([
            Change("match-1", ExternalUserChangeStatus.Pending, 1),
            Change("match-2", ExternalUserChangeStatus.Pending, 3),
            Change("other", ExternalUserChangeStatus.Pending, 4),
            Change("match-3", ExternalUserChangeStatus.Applied, 2)
        ]);
        var options = new ExternalUserChangeListQuery(ExternalUserChangeStatus.Pending, 1, 1,
            "match", "receivedAt", true, false);

        var result = _sut.List(_organization.Uuid, options);

        Assert.True(result.Ok);
        Assert.Equal(3, result.Value.PendingCount);
        Assert.Equal(2, result.Value.Total);
        Assert.Equal("match-1", Assert.Single(result.Value.Items).ExternalMessageId);
    }

    [Fact]
    public void List_ResolvedOnlyExcludesPending()
    {
        _changes.AddRange([
            Change("pending", ExternalUserChangeStatus.Pending, 1),
            Change("resolved", ExternalUserChangeStatus.Dismissed, 2)
        ]);

        var result = _sut.List(_organization.Uuid, new(null, 0, 50, null, "status", false, true));

        Assert.Equal(1, result.Value.PendingCount);
        Assert.Equal(1, result.Value.Total);
        Assert.Equal("resolved", Assert.Single(result.Value.Items).ExternalMessageId);
    }

    [Fact]
    public void List_SearchesMatchedUserAndSortsByName()
    {
        _changes.AddRange([
            Change("one", ExternalUserChangeStatus.Pending, 1),
            Change("two", ExternalUserChangeStatus.Pending, 2),
            Change("three", ExternalUserChangeStatus.Pending, 3)
        ]);
        _changes[0].User = new User { Name = "Zoe", LastName = "", Email = "zoe@example.invalid" };
        _changes[1].User = new User { Name = "Anna", LastName = "", Email = "anna@example.invalid" };
        _changes[2].User = new User { Name = "Charlie", LastName = "", Email = "charlie@example.invalid" };

        var result = _sut.List(_organization.Uuid,
            new(null, 0, 50, "example.invalid", "userName", false, false));

        Assert.Equal(3, result.Value.Total);
        Assert.Equal(["two", "three", "one"], result.Value.Items.Select(x => x.ExternalMessageId));
    }

    [Theory]
    [InlineData(-1, 50, "receivedAt", OperationFailure.BadInput)]
    [InlineData(0, 201, "receivedAt", OperationFailure.BadInput)]
    [InlineData(0, 50, "invalid", OperationFailure.BadInput)]
    public void List_InvalidQuery_IsRejected(int skip, int take, string sort, OperationFailure failure)
    {
        var result = _sut.List(_organization.Uuid, new(null, skip, take, null, sort, true, false));

        Assert.Equal(failure, result.Error.FailureType);
    }

    [Fact]
    public void Read_NonAdministrator_CannotSeeChanges()
    {
        _actor.Setup(x => x.HasRole(_organization.Id, OrganizationRole.LocalAdmin)).Returns(false);
        _changes.Add(Change("private", ExternalUserChangeStatus.Pending, 1));

        Assert.Equal(OperationFailure.Forbidden, _sut.PendingCount(_organization.Uuid).Error.FailureType);
        Assert.Equal(OperationFailure.Forbidden,
            _sut.Detail(_organization.Uuid, _changes[0].Uuid).Error.FailureType);
        Assert.Equal(OperationFailure.Forbidden,
            _sut.List(_organization.Uuid, new(null, 0, 50, null, "receivedAt", true, false)).Error.FailureType);
        _store.Verify(x => x.Query(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Detail_UnknownChange_ReturnsNotFound()
    {
        Assert.Equal(OperationFailure.NotFound,
            _sut.Detail(_organization.Uuid, Guid.NewGuid()).Error.FailureType);
    }

    private ExternalUserChange Change(string messageId, ExternalUserChangeStatus status, int day) => new()
    {
        OrganizationId = _organization.Id,
        ExternalMessageId = messageId,
        Status = status,
        ReceivedAt = new DateTime(2026, 9, day, 0, 0, 0, DateTimeKind.Utc)
    };
}
