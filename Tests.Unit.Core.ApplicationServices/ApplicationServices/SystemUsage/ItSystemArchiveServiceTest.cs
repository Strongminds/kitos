using Core.Abstractions.Types;
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

namespace Tests.Unit.Core.ApplicationServices.SystemUsage
{
    public class ItSystemArchiveServiceTest : WithAutoFixture
    {
        private readonly Mock<IItSystemUsageService> _systemUsageService;
        private readonly Mock<IGenericRepository<ItSystemArchive>> _archiveRepository;
        private readonly ItSystemArchiveService _sut;

        public ItSystemArchiveServiceTest()
        {
            _systemUsageService = new Mock<IItSystemUsageService>();
            _archiveRepository = new Mock<IGenericRepository<ItSystemArchive>>();
            _sut = new ItSystemArchiveService(_systemUsageService.Object, _archiveRepository.Object);
        }

        [Fact]
        public void Create_Propagates_Error_If_SystemUsage_Cannot_Be_Resolved()
        {
            // Arrange
            var usageUuid = A<Guid>();
            var error = new OperationError(OperationFailure.NotFound);
            var parameters = CreateParameters();
            _systemUsageService.Setup(x => x.GetItSystemUsageByUuid(usageUuid)).Returns(error);

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
            var organizationUuid = A<Guid>();
            var parameters = CreateParameters();
            parameters.ArchiveReferences = new[]
            {
                new ArchiveReferenceProperties { Label = "Ref 1", Url = "https://example.com/1" },
                new ArchiveReferenceProperties { Label = "Ref 2", Url = "https://example.com/2" }
            };

            var usage = new ItSystemUsage
            {
                Organization = new Organization { Uuid = organizationUuid },
                ItSystem = new ItSystem { Name = "Legacy Name" },
                LocalCallName = "Local Name",
                LocalSystemId = "Local Id"
            };

            ItSystemArchive insertedArchive = null;

            _systemUsageService.Setup(x => x.GetItSystemUsageByUuid(usageUuid)).Returns(usage);
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
            Assert.Equal(organizationUuid, insertedArchive.OrganizationUuid);
            Assert.Equal(parameters.ArchivingDate, insertedArchive.ArchivingDate);
            Assert.Equal(parameters.ReferenceName, insertedArchive.ReferenceName);
            Assert.Equal(parameters.Note, insertedArchive.Note);

            Assert.Equal("Legacy Name", insertedArchive.Snapshot.LegacyName);
            Assert.Equal("Local Name", insertedArchive.Snapshot.LocalName);
            Assert.Equal("Local Id", insertedArchive.Snapshot.LocalId);

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
            var usage = new ItSystemUsage
            {
                Organization = new Organization { Uuid = A<Guid>() },
                ItSystem = new ItSystem { Name = A<string>() }
            };

            ItSystemArchive insertedArchive = null;
            _systemUsageService.Setup(x => x.GetItSystemUsageByUuid(usageUuid)).Returns(usage);
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

        private ArchiveItSystemUsageParameters CreateParameters()
        {
            return new ArchiveItSystemUsageParameters
            {
                ArchivingDate = A<DateTime>(),
                ReferenceName = A<string>(),
                Note = A<string>()
            };
        }
    }
}
