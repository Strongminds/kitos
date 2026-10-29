using Core.DomainServices.Repositories.Organization;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices;
using Core.ApplicationServices.Organizations;
using Core.ApplicationServices.Rights;
using Core.ApplicationServices.Users;
using Core.ApplicationServices.Users.Handlers;
using Core.Abstractions.Types;
using Core.DomainModel;
using Core.DomainModel.Commands;
using Core.DomainModel.Events;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainModel.SSO;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.SSO;
using Core.DomainServices;
using Core.DomainServices.Authorization;
using Core.DomainServices.Generic;
using Core.DomainServices.Options;
using Core.DomainServices.Queries;
using Core.DomainServices.Role;
using Core.DomainServices.Users;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Repositories;
using Infrastructure.DataAccess.Services;
using Infrastructure.DataAccess.Migrations.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;
using Serilog;

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
        Migration[] migrations = [new AddExternalUserChanges()];
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
        Assert.Null(db.Model.FindEntityType(typeof(User))!.FindProperty("IsPubSubUser"));
        Assert.Equal(2, await db.OrganizationRights.CountAsync());
    }

    [Fact]
    public async Task ConcurrentDuplicateDeliveryCreatesExactlyOneRecord()
    {
        Guid Insert()
        {
            using var db = Context();
            return new ExternalUserChangeRepository(db).InsertAndSave(Change("same-message")).Uuid;
        }
        var ids = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(Insert)));
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
            uuid = new ExternalUserChangeRepository(db).InsertAndSave(Change("apply")).Uuid;
        await using (var db = Context())
            Assert.True(Resolver(db).Resolve(_organization.Uuid, uuid, true).Ok);
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
            Assert.True(Resolver(db).Resolve(_organization.Uuid, uuid, false).Failed);
    }

    [Fact]
    public async Task Ingestion_ConcurrentDifferentMessages_OnlyOnePendingChangeCommits()
    {
        await using (var db = Context())
        {
            var organization = await db.Organizations.SingleAsync(x => x.Id == _organization.Id);
            organization.FkOrgUsersConnected = true;
            await db.SaveChangesAsync();
        }
        using var readers = new Barrier(2);
        Result<ExternalUserChange, OperationError> Deliver(string messageId)
        {
            using var db = Context();
            var repository = new ExternalUserChangeRepository(db);
            var synchronized = new Mock<IExternalUserChangeRepository>();
            synchronized.Setup(x => x.FindByMessageId(messageId)).Returns(() =>
            {
                var existing = repository.FindByMessageId(messageId);
                Assert.True(readers.SignalAndWait(TimeSpan.FromSeconds(30)));
                return existing;
            });
            synchronized.Setup(x => x.AsQueryable()).Returns(() => repository.AsQueryable());
            synchronized.Setup(x => x.InsertAndSave(It.IsAny<ExternalUserChange>()))
                .Returns((ExternalUserChange change) => repository.InsertAndSave(change));
            var ingestion = new ExternalUserChangeIngestionService(synchronized.Object,
                new OrganizationRepository(new GenericRepository<Organization>(db)),
                new SsoUserIdentityRepository(new GenericRepository<SsoUserIdentity>(db)),
                new ExternalUserChangeIngestionTransaction(db));
            return ingestion.Ingest(new Core.ApplicationServices.Model.Users.ExternalUserChangeInput(
                messageId, _organization.Uuid, _externalUuid, ExternalUserChangeType.Deleted, DateTime.UtcNow));
        }

        var results = await Task.WhenAll(Task.Run(() => Deliver("first")), Task.Run(() => Deliver("second")));

        Assert.Single(results, x => x.Ok);
        Assert.Equal(OperationFailure.UnknownError, Assert.Single(results, x => x.Failed).Error.FailureType);
        await using var verify = Context();
        Assert.Single(await verify.Set<ExternalUserChange>().ToListAsync());
    }

    [Fact]
    public async Task ApplyingLastOrganizationDeletionMarksUserDeleted()
    {
        await using var db = Context();
        db.OrganizationRights.RemoveRange(await db.OrganizationRights.Where(x => x.OrganizationId == _otherOrganization.Id).ToListAsync());
        await db.SaveChangesAsync();
        var change = new ExternalUserChangeRepository(db).InsertAndSave(Change("last-org"));
        Assert.True(Resolver(db).Resolve(_organization.Uuid, change.Uuid, true).Ok);
        await using var verify = Context();
        var deletedUser = await verify.Users.SingleAsync(x => x.Id == _user.Id);
        Assert.True(deletedUser.Deleted);
        Assert.Equal("Slettet bruger", deletedUser.Name);
        Assert.NotEqual(_user.Email, deletedUser.Email);
        Assert.Null(deletedUser.PhoneNumber);
        Assert.Empty(await verify.SsoUserIdentities.Where(x => x.User.Id == _user.Id).ToListAsync());
        Assert.Empty(await verify.OrganizationRights.Where(x => x.UserId == _user.Id).ToListAsync());
        Assert.Equal(ExternalUserChangeStatus.Applied, (await verify.Set<ExternalUserChange>().SingleAsync()).Status);
    }

    [Fact]
    public async Task Resolve_Apply_RemovesAllGrantTypesOnlyInTargetOrganization()
    {
        await using (var db = Context())
        {
            await AddBusinessRights(db);
            var change = new ExternalUserChangeRepository(db).InsertAndSave(Change("all-rights"));
            Assert.True(Resolver(db).Resolve(_organization.Uuid, change.Uuid, true).Ok);
        }

        await using var verify = Context();
        Assert.Equal(_otherOrganization.Id, (await verify.OrganizationRights.SingleAsync(x => x.UserId == _user.Id)).OrganizationId);
        Assert.Equal(_otherOrganization.Id, (await verify.Set<DataProcessingRegistrationRight>().SingleAsync()).Object.OrganizationId);
        Assert.Equal(_otherOrganization.Id, (await verify.Set<ItContractRight>().SingleAsync()).Object.OrganizationId);
        Assert.Equal(_otherOrganization.Id, (await verify.Set<ItSystemRight>().SingleAsync()).Object.OrganizationId);
        Assert.Equal(_otherOrganization.Id, (await verify.Set<OrganizationUnitRight>().SingleAsync()).Object.OrganizationId);
        Assert.False((await verify.Users.SingleAsync(x => x.Id == _user.Id)).Deleted);
        Assert.Equal(ExternalUserChangeStatus.Applied, (await verify.Set<ExternalUserChange>().SingleAsync()).Status);
    }

    [Fact]
    public async Task Resolve_RemovalFailsAfterBusinessRightsSaved_RollsBackAllGrantsAndResolution()
    {
        await using (var db = Context())
        {
            await AddBusinessRights(db);
            var change = new ExternalUserChangeRepository(db).InsertAndSave(Change("rollback-rights"));
            var result = Resolver(db, allowMembershipRemoval: false).Resolve(_organization.Uuid, change.Uuid, true);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        await using var verify = Context();
        Assert.Equal(2, await verify.OrganizationRights.CountAsync(x => x.UserId == _user.Id));
        Assert.Equal(2, await verify.Set<DataProcessingRegistrationRight>().CountAsync());
        Assert.Equal(2, await verify.Set<ItContractRight>().CountAsync());
        Assert.Equal(2, await verify.Set<ItSystemRight>().CountAsync());
        Assert.Equal(2, await verify.Set<OrganizationUnitRight>().CountAsync());
        var user = await verify.Users.SingleAsync(x => x.Id == _user.Id);
        Assert.False(user.Deleted);
        Assert.Null(user.DeletedDate);
        var pending = await verify.Set<ExternalUserChange>().SingleAsync();
        Assert.Equal(ExternalUserChangeStatus.Pending, pending.Status);
        Assert.Null(pending.ResolvedAt);
    }

    [Fact]
    public async Task ConcurrentApplyAndDismissHaveOnlyOneWinner()
    {
        Guid uuid;
        await using (var db = Context())
            uuid = new ExternalUserChangeRepository(db).InsertAndSave(Change("race")).Uuid;
        bool Resolve(bool apply)
        {
            using var db = Context();
            return Resolver(db).Resolve(_organization.Uuid, uuid, apply).Ok;
        }
        var results = await Task.WhenAll(Task.Run(() => Resolve(true)), Task.Run(() => Resolve(false)));
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
            uuid = new ExternalUserChangeRepository(db).InsertAndSave(change).Uuid;
        }
        await using (var db = Context())
            Assert.True(Resolver(db).Resolve(_organization.Uuid, uuid, true).Failed);
        await using (var db = Context())
            Assert.True(Resolver(db).Resolve(_otherOrganization.Uuid, uuid, true).Failed);
        await using (var db = Context())
            Assert.True(Resolver(db).Resolve(_organization.Uuid, uuid, false).Ok);
        await using var verify = Context();
        Assert.Equal(2, await verify.OrganizationRights.CountAsync(x => x.UserId == _user.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Ingestion_CompetingScopes_CreateOnePendingReviewAndPreserveResolution(bool apply)
    {
        await using (var db = Context())
        {
            var organization = await db.Organizations.SingleAsync(x => x.Id == _organization.Id);
            organization.FkOrgUsersConnected = true;
            await db.SaveChangesAsync();
        }
        var input = new Core.ApplicationServices.Model.Users.ExternalUserChangeInput("direct", _organization.Uuid,
            _externalUuid, ExternalUserChangeType.Deleted, DateTime.UtcNow);
        void Deliver()
        {
            using var db = Context();
            var ingestion = new ExternalUserChangeIngestionService(new ExternalUserChangeRepository(db),
                new OrganizationRepository(new GenericRepository<Organization>(db)),
                new SsoUserIdentityRepository(new GenericRepository<SsoUserIdentity>(db)), new ExternalUserChangeIngestionTransaction(db));
            var result = ingestion.Ingest(input);
            if (result.Failed)
            {
                Assert.Equal(OperationFailure.UnknownError, result.Error.FailureType);
                result = ingestion.Ingest(input);
            }
            Assert.True(result.Ok);
        }
        await Task.WhenAll(Task.Run(Deliver), Task.Run(Deliver));
        await using (var check = Context())
        {
            var pending = Assert.Single(await check.Set<ExternalUserChange>().ToListAsync());
            Assert.Equal(ExternalUserChangeStatus.Pending, pending.Status);
            Assert.Equal(_user.Id, pending.UserId);
            Assert.Equal(2, await check.OrganizationRights.CountAsync(x => x.UserId == _user.Id));
            Assert.True(Resolver(check).Resolve(_organization.Uuid, pending.Uuid, apply).Ok);
        }
        Deliver(); // Lost acknowledgement after commit must not reset administrator resolution.
        await using var verify = Context();
        var resolved = Assert.Single(await verify.Set<ExternalUserChange>().ToListAsync());
        Assert.Equal(apply ? ExternalUserChangeStatus.Applied : ExternalUserChangeStatus.Dismissed, resolved.Status);
        Assert.Equal(apply ? 1 : 2, await verify.OrganizationRights.CountAsync(x => x.UserId == _user.Id));
    }
    private ExternalUserChangeResolutionService Resolver(KitosContext db, bool allowMembershipRemoval = true)
    {
        var actor = new OrganizationalUserContext(_actor.Id,
            new Dictionary<int, IEnumerable<OrganizationRole>> { [_organization.Id] = [OrganizationRole.LocalAdmin] },
            new Dictionary<int, OrganizationCategory> { [_organization.Id] = OrganizationCategory.Municipality }, false, false, false);
        var authorization = new Mock<IAuthorizationContext>();
        authorization.Setup(x => x.AllowModify(It.IsAny<IEntity>())).Returns(true);
        authorization.Setup(x => x.AllowDelete(It.IsAny<IEntity>())).Returns(allowMembershipRemoval);
        var organizations = new Mock<IOrganizationService>();
        organizations.Setup(x => x.GetOrganization(_organization.Uuid, OrganizationDataReadAccessLevel.All))
            .Returns(() => db.Organizations.Single(x => x.Id == _organization.Id));
        var users = new Mock<IUserService>();
        users.Setup(x => x.GetUsersInOrganization(_organization.Uuid, It.IsAny<IDomainQuery<User>[]>()))
            .Returns(() => Result<IQueryable<User>, OperationError>.Success(db.Users.Where(x => x.Id == _user.Id)));
        var identities = new Mock<IEntityIdentityResolver>();
        identities.Setup(x => x.ResolveUuid<Organization>(_organization.Id)).Returns(_organization.Uuid);
        var transactionManager = new TransactionManager(db);
        var events = new NoEvents();
        var organizationRights = new OrganizationRightsService(authorization.Object,
            new GenericRepository<OrganizationRight>(db), actor, events, Mock.Of<ILogger>(),
            new GenericRepository<OrganizationUnitRight>(db), transactionManager, new GenericRepository<Organization>(db));
        var userRights = new UserRightsService(users.Object, organizations.Object, authorization.Object,
            identities.Object, organizationRights,
            RoleAssignments<ItContractRight, ItContractRole, ItContract>(db),
            RoleAssignments<ItSystemRight, ItSystemRole, ItSystemUsage>(db),
            RoleAssignments<OrganizationUnitRight, OrganizationUnitRole, OrganizationUnit>(db),
            RoleAssignments<DataProcessingRegistrationRight, DataProcessingRegistrationRole, DataProcessingRegistration>(db),
            transactionManager, new EntityFrameworkContextDatabaseControl(db), Mock.Of<ILogger>());
        var ssoIdentities = new SsoUserIdentityRepository(new GenericRepository<SsoUserIdentity>(db));
        var commandBus = new Mock<ICommandBus>(MockBehavior.Strict);
        var deletionHandler = new RemoveUserFromKitosCommandHandler(ssoIdentities, commandBus.Object, events);
        commandBus.Setup(x => x.Execute<RemoveUserFromKitosCommand, Maybe<OperationError>>(It.IsAny<RemoveUserFromKitosCommand>()))
            .Returns((RemoveUserFromKitosCommand command) => deletionHandler.Execute(command));
        return new ExternalUserChangeResolutionService(new ExternalUserChangeResolutionTransaction(db),
            new GenericRepository<Organization>(db), new GenericRepository<ExternalUserChange>(db),
            new GenericRepository<OrganizationRight>(db), actor,
            ssoIdentities, events, userRights, commandBus.Object);
    }

    private RoleAssignmentService<TRight, TRole, TModel> RoleAssignments<TRight, TRole, TModel>(KitosContext db)
        where TRight : Entity, IRight<TModel, TRight, TRole>
        where TRole : OptionEntity<TRight>, IRoleEntity, IOptionReference<TRight>
        where TModel : HasRightsEntity<TModel, TRight, TRole>, IOwnedByOrganization
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetById(_user.Id)).Returns(() => db.Users.Single(x => x.Id == _user.Id));
        var options = new Mock<IOptionsService<TRight, TRole>>();
        options.Setup(x => x.GetOption(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int _, int roleId) => Maybe<(TRole option, bool available)>.Some((db.Set<TRole>().Single(x => x.Id == roleId), true)));
        return new RoleAssignmentService<TRight, TRole, TModel>(options.Object, users.Object,
            new GenericRepository<TRight>(db), new TransactionManager(db), new NoEvents());
    }

    private async Task AddBusinessRights(KitosContext db)
    {
        var user = await db.Users.SingleAsync(x => x.Id == _user.Id);
        var organizations = await db.Organizations.ToListAsync();
        foreach (var organization in organizations)
        {
            var dpr = new DataProcessingRegistration
            {
                Name = "DPR",
                Organization = organization,
                ObjectOwnerId = _actor.Id,
                LastChangedByUserId = _actor.Id
            };
            var contract = new ItContract
            {
                Name = "Contract",
                Organization = organization,
                ObjectOwnerId = _actor.Id,
                LastChangedByUserId = _actor.Id
            };
            var system = new ItSystem
            {
                Name = "System",
                Organization = organization,
                ObjectOwnerId = _actor.Id,
                LastChangedByUserId = _actor.Id
            };
            var usage = new ItSystemUsage
            {
                ItSystem = system,
                Organization = organization,
                ObjectOwnerId = _actor.Id,
                LastChangedByUserId = _actor.Id
            };
            var unit = new OrganizationUnit
            {
                Name = "Unit",
                Organization = organization,
                ObjectOwnerId = _actor.Id,
                LastChangedByUserId = _actor.Id
            };
            db.Add(dpr.CreateNewRight(new DataProcessingRegistrationRole { Name = "DPR role" }, user));
            db.Add(contract.CreateNewRight(new ItContractRole { Name = "Contract role" }, user));
            db.Add(usage.CreateNewRight(new ItSystemRole { Name = "System role" }, user));
            db.Add(unit.CreateNewRight(new OrganizationUnitRole { Name = "Unit role" }, user));
        }
        foreach (var entry in db.ChangeTracker.Entries<Entity>().Where(x => x.State == EntityState.Added))
        {
            entry.Entity.ObjectOwnerId = _actor.Id;
            entry.Entity.LastChangedByUserId = _actor.Id;
        }
        await db.SaveChangesAsync();
    }

    private ExternalUserChange Change(string id) => new()
    {
        ExternalMessageId = id,
        OrganizationId = _organization.Id,
        ExternalUserUuid = _externalUuid,
        UserId = _user.Id,
        ChangeType = ExternalUserChangeType.Deleted,
        ReceivedAt = DateTime.UtcNow
    };
    private OrganizationRight Right(int organizationId) => new()
    {
        UserId = _user.Id,
        OrganizationId = organizationId,
        Role = OrganizationRole.User,
        ObjectOwnerId = _actor.Id,
        LastChangedByUserId = _actor.Id
    };
    private static User NewUser(string name) => new() { Name = name, Email = name + "@test.invalid", Password = "test", Salt = "test" };
    private class NoEvents : IDomainEvents { public void Raise<T>(T args) where T : IDomainEvent { } }
}
