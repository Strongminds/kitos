using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.SystemUsage;
using Core.DomainModel.Archive;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.ApplicationServices.SystemUsage;
using Core.DomainServices;
using Moq;
using System;
using System.Linq;
using Tests.Toolkit.Patterns;
using Xunit;
using Core.ApplicationServices.Model.SystemUsage.Write;

namespace Tests.Unit.Core.ApplicationServices.SystemUsage
{
    public class ItSystemArchiveServiceTest : WithAutoFixture
    {
        private readonly Mock<IItSystemUsageService> _systemUsageService;
        private readonly Mock<IAuthorizationContext> _authorizationContext;
        private readonly Mock<IGenericRepository<ItSystemArchive>> _archiveRepository;
        private readonly ItSystemArchiveService _sut;

        public ItSystemArchiveServiceTest()
        {
            _systemUsageService = new Mock<IItSystemUsageService>();
            _authorizationContext = new Mock<IAuthorizationContext>();
            _archiveRepository = new Mock<IGenericRepository<ItSystemArchive>>();
            _sut = new ItSystemArchiveService(_systemUsageService.Object, _authorizationContext.Object, _archiveRepository.Object);
        }

        [Fact]
        public void Create_Propagates_Error_If_SystemUsage_Cannot_Be_Resolved()
        {
            // Arrange
            var usageUuid = A<Guid>();
            var error = new OperationError(OperationFailure.NotFound);
            var parameters = CreateParameters();
            _systemUsageService.Setup(x => x.GetItSystemUsageByUuidAndAuthorizeRead(usageUuid)).Returns(error);

            // Act
            var result = _sut.Create(usageUuid, parameters);

            // Assert
            Assert.True(result.Failed);
            Assert.Same(error, result.Error);
            _archiveRepository.Verify(x => x.Insert(It.IsAny<ItSystemArchive>()), Times.Never);
            _archiveRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public void Create_Creates_Archive_With_Snapshot_And_References()
        {
            // Arrange
            var usageUuid = A<Guid>();
            var parameters = CreateParameters();
            parameters.ArchiveReferences = new[]
            {
                new NamedLink("Ref 1", "https://example.com/1"),
                new NamedLink("Ref 2", "https://example.com/2")
            };

            var usage = CreateSystemUsage("Legacy Name", "Local Name", "Local Id");

            ItSystemArchive insertedArchive = null;

            SetupSystemUsageLookup(usageUuid, usage);
            SetupAllowCreate(usage.OrganizationId, true);
            _archiveRepository
                .Setup(x => x.Insert(It.IsAny<ItSystemArchive>()))
                .Callback<ItSystemArchive>(archive => insertedArchive = archive)
                .Returns<ItSystemArchive>(archive => archive);

            // Act
            var result = _sut.Create(usageUuid, parameters);

            // Assert
            Assert.True(result.Ok);
            Assert.Same(insertedArchive, result.Value);
            Assert.NotNull(insertedArchive);
            Assert.Equal(usage.OrganizationId, insertedArchive.OrganizationId);
            Assert.Equal(parameters.ArchivingDate, insertedArchive.ArchivingDate);
            Assert.Equal(parameters.ReferenceName, insertedArchive.ReferenceName);
            Assert.Equal(parameters.Note, insertedArchive.Note);
            Assert.NotEqual(Guid.Empty, insertedArchive.SnapshotUuid);
            Assert.Equal(insertedArchive.Snapshot.Uuid, insertedArchive.SnapshotUuid);

            Assert.Equal(usage.ItSystem.Uuid, insertedArchive.Snapshot.ItSystemUuid);
            Assert.Equal("Legacy Name", insertedArchive.Snapshot.LegacyName);
            Assert.Equal("Local Name", insertedArchive.Snapshot.LocalName);
            Assert.Equal("Local Id", insertedArchive.Snapshot.LocalId);
            Assert.Equal(usage.Concluded, insertedArchive.Snapshot.TakenIntoUsageDate);

            var mappedReferences = insertedArchive.ArchiveReferences.ToList();
            Assert.Equal(2, mappedReferences.Count);
            Assert.Contains(mappedReferences, reference => reference.Label == "Ref 1" && reference.Url == "https://example.com/1");
            Assert.Contains(mappedReferences, reference => reference.Label == "Ref 2" && reference.Url == "https://example.com/2");
            _archiveRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public void Create_Uses_Empty_Reference_Collection_If_Request_References_Are_Null()
        {
            // Arrange
            var usageUuid = A<Guid>();
            var parameters = CreateParameters();
            parameters.ArchiveReferences = null!;
            var usage = CreateSystemUsage();

            ItSystemArchive insertedArchive = null;
            SetupSystemUsageLookup(usageUuid, usage);
            SetupAllowCreate(usage.OrganizationId, true);
            _archiveRepository
                .Setup(x => x.Insert(It.IsAny<ItSystemArchive>()))
                .Callback<ItSystemArchive>(archive => insertedArchive = archive)
                .Returns<ItSystemArchive>(archive => archive);

            // Act
            var result = _sut.Create(usageUuid, parameters);

            // Assert
            Assert.True(result.Ok);
            Assert.NotNull(insertedArchive);
            Assert.Empty(insertedArchive.ArchiveReferences);
            _archiveRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public void Create_Returns_Forbidden_If_User_Cannot_Create_Archive()
        {
            // Arrange
            var usageUuid = A<Guid>();
            var parameters = CreateParameters();
            var usage = CreateSystemUsage();
            SetupSystemUsageLookup(usageUuid, usage);
            SetupAllowCreate(usage.OrganizationId, false);

            // Act
            var result = _sut.Create(usageUuid, parameters);

            // Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
            _archiveRepository.Verify(x => x.Insert(It.IsAny<ItSystemArchive>()), Times.Never);
            _archiveRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public void GetByUuid_Returns_NotFound_If_Archive_Does_Not_Exist()
        {
            // Arrange
            var archiveUuid = A<Guid>();
            SetupArchiveQuery();

            // Act
            var result = _sut.GetByUuid(archiveUuid);

            // Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.NotFound, result.Error.FailureType);
        }

        [Fact]
        public void GetByUuid_Returns_Forbidden_If_User_Cannot_Read_Archive()
        {
            // Arrange
            var archiveUuid = A<Guid>();
            var archive = CreateArchive(archiveUuid);

            SetupArchiveQuery(archive);
            SetupAllowReads(archive, false);

            // Act
            var result = _sut.GetByUuid(archiveUuid);

            // Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        [Fact]
        public void GetByUuid_Returns_Archive_If_User_Can_Read()
        {
            // Arrange
            var archiveUuid = A<Guid>();
            var archive = CreateArchive(archiveUuid);

            SetupArchiveQuery(archive);
            SetupAllowReads(archive, true);

            // Act
            var result = _sut.GetByUuid(archiveUuid);

            // Assert
            Assert.True(result.Ok);
            Assert.Same(archive, result.Value);
        }

        [Fact]
        public void Delete_Returns_NotFound_If_Archive_Does_Not_Exist()
        {
            // Arrange
            var archiveUuid = A<Guid>();
            SetupArchiveQuery();

            // Act
            var result = _sut.Delete(archiveUuid);

            // Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.NotFound, result.Error.FailureType);
            _archiveRepository.Verify(x => x.Delete(It.IsAny<ItSystemArchive>()), Times.Never);
            _archiveRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public void Delete_Returns_Forbidden_If_User_Cannot_Delete_Archive()
        {
            // Arrange
            var archiveUuid = A<Guid>();
            var archive = CreateArchive(archiveUuid);

            SetupArchiveQuery(archive);
            SetupAllowReads(archive, true);
            SetupAllowDelete(archive, false);

            // Act
            var result = _sut.Delete(archiveUuid);

            // Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
            VerifyAllowDelete(archive, Times.Once());
            _archiveRepository.Verify(x => x.Delete(It.IsAny<ItSystemArchive>()), Times.Never);
            _archiveRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public void Delete_Deletes_Archive_If_User_Can_Delete()
        {
            // Arrange
            var archiveUuid = A<Guid>();
            var archive = CreateArchive(archiveUuid);

            SetupArchiveQuery(archive);
            SetupAllowReads(archive, true);
            SetupAllowDelete(archive, true);

            // Act
            var result = _sut.Delete(archiveUuid);

            // Assert
            Assert.True(result.Ok);
            Assert.Same(archive, result.Value);
            _archiveRepository.Verify(x => x.Delete(archive), Times.Once);
            _archiveRepository.Verify(x => x.Save(), Times.Once);
        }

        private ArchiveItSystemUsageParameters CreateParameters()
        {
            return new ArchiveItSystemUsageParameters
            {
                ArchivingDate = A<DateTime>(),
                TakenIntoUsageDate = A<DateTime>(),
                ReferenceName = A<string>(),
                Note = A<string>()
            };
        }

        private void SetupSystemUsageLookup(Guid usageUuid, ItSystemUsage usage)
        {
            _systemUsageService.Setup(x => x.GetItSystemUsageByUuidAndAuthorizeRead(usageUuid)).Returns(usage);
        }

        private void SetupArchiveQuery(params ItSystemArchive[] archives)
        {
            _archiveRepository.Setup(x => x.AsQueryable()).Returns(archives.AsQueryable());
        }

        private void SetupAllowCreate(int organizationId, bool isAllowed)
        {
            _authorizationContext.Setup(x => x.AllowCreate<ItSystemArchive>(organizationId)).Returns(isAllowed);
        }

        private void SetupAllowReads(ItSystemArchive archive, bool isAllowed)
        {
            _authorizationContext.Setup(x => x.AllowReads(archive)).Returns(isAllowed);
        }

        private void SetupAllowDelete(ItSystemArchive archive, bool isAllowed)
        {
            _authorizationContext.Setup(x => x.AllowDelete(archive)).Returns(isAllowed);
        }

        private void VerifyAllowDelete(ItSystemArchive archive, Times times)
        {
            _authorizationContext.Verify(x => x.AllowDelete(archive), times);
        }

        private ItSystemUsage CreateSystemUsage(string? systemName = null, string? localName = null, string? localId = null)
        {
            return new ItSystemUsage
            {
                OrganizationId = A<int>(),
                Organization = new Organization { Uuid = A<Guid>() },
                ItSystem = new ItSystem { Uuid = A<Guid>(), Name = systemName ?? A<string>() },
                LocalCallName = localName,
                LocalSystemId = localId
            };
        }

        private ItSystemArchive CreateArchive(Guid archiveUuid)
        {
            return new ItSystemArchive
            {
                Uuid = archiveUuid,
                OrganizationId = A<int>(),
                Organization = new Organization { Uuid = A<Guid>() },
                ReferenceName = A<string>(),
                Note = A<string>(),
                ArchivingDate = A<DateTime>(),
                Snapshot = new ItSystemUsageArchiveSnapshot()
            };
        }
    }
}
