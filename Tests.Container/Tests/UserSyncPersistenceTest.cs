using Core.ApplicationServices.Authorization;
using Core.DomainModel;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainModel.SSO;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.SSO;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Repositories;
using Infrastructure.DataAccess.Migrations.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Presentation.Web.Services;

namespace Tests.Container.Tests;

[Collection(MigrationTestsCollection.Name)]
public sealed class UserSyncPersistenceTest : IAsyncLifetime
{
    private readonly IContainer _postgres = new ContainerBuilder("postgres:16-alpine")
        .WithEnvironment("POSTGRES_PASSWORD", "test-only")
        .WithEnvironment("POSTGRES_DB", "user_sync_test")
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432)).Build();
    private Organization _organization = null!;
    private Organization _otherOrganization = null!;
    private User _actor = null!;
    private User _user = null!;
    private readonly Guid _externalUuid = Guid.NewGuid();

    private KitosContext Context() => new(new DbContextOptionsBuilder<KitosContext>()
        .UseLazyLoadingProxies()
        .UseNpgsql($"Host={_postgres.Hostname};Port={_postgres.GetMappedPublicPort(5432)};Database=user_sync_test;Username=postgres;Password=test-only")
        .ReplaceService<IMigrationsSqlGenerator, KitosNpgsqlMigrationsSqlGenerator>()
        .Options);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = Context();
        // The legacy mappings reuse index names across tables. Route schema creation
        // through the same name normalization used by the production migrations.
        var model = db.GetService<IDesignTimeModel>().Model;
        var operations = db.GetService<IMigrationsModelDiffer>()
            .GetDifferences(null, model.GetRelationalModel());
        foreach (var index in operations.OfType<CreateIndexOperation>())
            index.Schema = null;
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(operations, model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await db.Database.OpenConnectionAsync();
        await ((Npgsql.NpgsqlConnection)db.Database.GetDbConnection()).ReloadTypesAsync();
        _actor = NewUser("admin");
        _user = NewUser("member");
        db.Users.AddRange(_actor, _user);
        await db.SaveChangesAsync();
        var type = new OrganizationType { Name = "Municipality", Category = OrganizationCategory.Municipality };
        _organization = new Organization { Name = "First", Type = type, ObjectOwnerId = _actor.Id, LastChangedByUserId = _actor.Id };
        _otherOrganization = new Organization { Name = "Second", Type = type, ObjectOwnerId = _actor.Id, LastChangedByUserId = _actor.Id };
        db.Organizations.AddRange(_organization, _otherOrganization);
        await db.SaveChangesAsync();
        db.OrganizationRights.AddRange(Right(_organization.Id), Right(_otherOrganization.Id));
        db.SsoUserIdentities.Add(new SsoUserIdentity(_externalUuid, _user));
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task UserSyncMigrationsRoundTripWithoutChangingUsersOrRoles()
    {
        await using var db = Context();
        Migration[] migrations = [new AddExternalUserChanges(), new AddPubSubUser()];
        var sql = db.GetService<IMigrationsSqlGenerator>();
        // Model-generated schema supplies the baseline; exercise the actual new Down and Up operations.
        foreach (var migration in migrations.Reverse())
            foreach (var command in sql.Generate(migration.DownOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
        foreach (var migration in migrations)
            foreach (var command in sql.Generate(migration.UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
        Assert.Empty(await db.Set<ExternalUserChange>().ToListAsync());
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.All(await db.Users.ToListAsync(), user => Assert.False(user.IsPubSubUser));
        Assert.Equal(2, await db.OrganizationRights.CountAsync());
    }

    [Fact]
    public async Task ConcurrentDuplicateDeliveryCreatesExactlyOneRecord()
    {
        async Task<Guid> Insert()
        {
            await using var db = Context();
            return (await new ExternalUserChangeStore(db).Insert(Change("same-message"), default)).Uuid;
        }
        var ids = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Insert()));
        Assert.Single(ids.Distinct());
        await using var verify = Context();
        Assert.Equal(1, await verify.Set<ExternalUserChange>().CountAsync());
        Assert.Equal(2, await verify.OrganizationRights.CountAsync(x => x.UserId == _user.Id));
    }

    [Fact]
    public async Task ApplyRemovesOrganizationRolesAndPreservesOtherOrganizationAccess()
    {
        Guid uuid;
        await using (var db = Context())
            uuid = (await new ExternalUserChangeStore(db).Insert(Change("apply"), default)).Uuid;
        await using (var db = Context())
            Assert.True((await Resolver(db).Resolve(_organization.Uuid, uuid, true, default)).Ok);
        await using (var db = Context())
        {
            Assert.False(await db.OrganizationRights.AnyAsync(x => x.UserId == _user.Id && x.OrganizationId == _organization.Id));
            Assert.True(await db.OrganizationRights.AnyAsync(x => x.UserId == _user.Id && x.OrganizationId == _otherOrganization.Id));
            Assert.Equal(1, await db.OrganizationRights.CountAsync(x => x.UserId == _user.Id));
            Assert.False((await db.Users.SingleAsync(x => x.Id == _user.Id)).Deleted);
            var change = await db.Set<ExternalUserChange>().SingleAsync();
            Assert.Equal(ExternalUserChangeStatus.Applied, change.Status);
            Assert.Equal(_actor.Id, change.ResolvedByUserId);
            Assert.NotNull(change.ResolvedAt);
        }
        await using (var db = Context())
            Assert.True((await Resolver(db).Resolve(_organization.Uuid, uuid, false, default)).Failed);
    }

    [Fact]
    public async Task ApplyingLastOrganizationDeletionMarksUserDeleted()
    {
        await using var db = Context();
        db.OrganizationRights.RemoveRange(await db.OrganizationRights.Where(x => x.OrganizationId == _otherOrganization.Id).ToListAsync());
        await db.SaveChangesAsync();
        var change = await new ExternalUserChangeStore(db).Insert(Change("last-org"), default);
        Assert.True((await Resolver(db).Resolve(_organization.Uuid, change.Uuid, true, default)).Ok);
        await using var verify = Context();
        Assert.True((await verify.Users.SingleAsync(x => x.Id == _user.Id)).Deleted);
        Assert.Empty(await verify.OrganizationRights.Where(x => x.UserId == _user.Id).ToListAsync());
        Assert.Equal(ExternalUserChangeStatus.Applied, (await verify.Set<ExternalUserChange>().SingleAsync()).Status);
    }

    [Fact]
    public async Task ConcurrentApplyAndDismissHaveOnlyOneWinner()
    {
        Guid uuid;
        await using (var db = Context())
            uuid = (await new ExternalUserChangeStore(db).Insert(Change("race"), default)).Uuid;
        async Task<bool> Resolve(bool apply)
        {
            await using var db = Context();
            return (await Resolver(db).Resolve(_organization.Uuid, uuid, apply, default)).Ok;
        }
        var results = await Task.WhenAll(Resolve(true), Resolve(false));
        Assert.Single(results, x => x);
        await using var verify = Context();
        var change = await verify.Set<ExternalUserChange>().SingleAsync();
        Assert.Equal(change.Status == ExternalUserChangeStatus.Applied,
            !await verify.OrganizationRights.AnyAsync(x => x.UserId == _user.Id && x.OrganizationId == _organization.Id));
    }

    [Fact]
    public async Task MissingBindingAndWrongOrganizationCannotBeApplied()
    {
        Guid uuid;
        await using (var db = Context())
        {
            var change = Change("unmatched");
            change.ExternalUserUuid = Guid.NewGuid();
            change.UserId = null;
            uuid = (await new ExternalUserChangeStore(db).Insert(change, default)).Uuid;
        }
        await using (var db = Context())
            Assert.True((await Resolver(db).Resolve(_organization.Uuid, uuid, true, default)).Failed);
        await using (var db = Context())
            Assert.True((await Resolver(db).Resolve(_otherOrganization.Uuid, uuid, true, default)).Failed);
        await using (var db = Context())
            Assert.True((await Resolver(db).Resolve(_organization.Uuid, uuid, false, default)).Ok);
        await using var verify = Context();
        Assert.Equal(2, await verify.OrganizationRights.CountAsync(x => x.UserId == _user.Id));
    }

    private ExternalUserChangeResolutionService Resolver(KitosContext db) => new(db,
        new OrganizationalUserContext(_actor.Id,
            new Dictionary<int, IEnumerable<OrganizationRole>> { [_organization.Id] = [OrganizationRole.LocalAdmin] },
            new Dictionary<int, OrganizationCategory> { [_organization.Id] = OrganizationCategory.Municipality }, false, false, false),
        new SsoUserIdentityRepository(new GenericRepository<SsoUserIdentity>(db)), new NoEvents());

    private ExternalUserChange Change(string id) => new()
    {
        ExternalMessageId = id, OrganizationId = _organization.Id, ExternalUserUuid = _externalUuid,
        UserId = _user.Id, ChangeType = ExternalUserChangeType.Deleted, ReceivedAt = DateTime.UtcNow
    };
    private OrganizationRight Right(int organizationId) => new()
    {
        UserId = _user.Id, OrganizationId = organizationId, Role = OrganizationRole.User,
        ObjectOwnerId = _actor.Id, LastChangedByUserId = _actor.Id
    };
    private static User NewUser(string name) => new() { Name = name, Email = name + "@test.invalid", Password = "test", Salt = "test" };
    private class NoEvents : IDomainEvents { public void Raise<T>(T args) where T : IDomainEvent { } }
}
