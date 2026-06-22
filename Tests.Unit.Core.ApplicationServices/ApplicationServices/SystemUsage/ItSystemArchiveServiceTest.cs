using Core.Abstractions.Types;
using Core.ApplicationServices.Model.SystemUsage;
using Core.DomainModel.Archive;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Generic;
using Core.DomainServices.Repositories.SystemUsage;
using Moq;
using System;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.SystemUsage
{
    public class ItSystemArchiveServiceTest: WithAutoFixture
    {
        private Mock<IItSystemUsageRepository> _usageRepository;
        private Mock<IEntityIdentityResolver> _identityResolver;
        private Mock<IGenericRepository<ItSystemArchive>> _archiveRepository;
        private Mock<IGenericRepository<Organization>> _organizationRepository;
        private ItSystemArchiveService _sut;

        public ItSystemArchiveServiceTest()
        {
            _usageRepository = new Mock<IItSystemUsageRepository>();
            _identityResolver = new Mock<IEntityIdentityResolver>();
            _archiveRepository = new Mock<IGenericRepository<ItSystemArchive>>();
            _organizationRepository = new Mock<IGenericRepository<Organization>>();
            _sut = new ItSystemArchiveService(
                _usageRepository.Object,
                _identityResolver.Object,
                _archiveRepository.Object,
                _organizationRepository.Object);
        }

        [Fact]
        public void Create_ReturnsNotFound_IfIdNotFound()
        {
            var usageUuid = A<Guid>();
            var parameters = new ArchiveItSystemUsageParameters
            {
                ArchivingDate = DateTime.UtcNow,
                ReferenceName = A<string>(),
                Note = A<string>()
            };
            _identityResolver.Setup(x => x.ResolveDbId<ItSystemUsage>(usageUuid)).Returns(Maybe<int>.None);

            var result = _sut.Create(usageUuid, parameters);

            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.NotFound, result.Error.FailureType);
        }

        [Fact]
        public void Create_ReturnsNotFound_IfUsageNotFoundInRepository()
        {
            var usageUuid = A<Guid>();
            var usageId = A<int>();
            var parameters = new ArchiveItSystemUsageParameters
            {
                ArchivingDate = DateTime.UtcNow,
                ReferenceName = A<string>(),
                Note = A<string>()
            };
            _identityResolver.Setup(x => x.ResolveDbId<ItSystemUsage>(usageUuid)).Returns(usageId);
            _usageRepository.Setup(x => x.GetSystemUsage(usageId)).Returns((ItSystemUsage)null);

            var result = _sut.Create(usageUuid, parameters);

            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.NotFound, result.Error.FailureType);
        }

        [Fact]
        public void Create_ReturnsNewArchive()
        {
            var usageUuid = A<Guid>();
            var usageId = A<int>();
            var organizationId = A<int>();
            var organizationUuid = A<Guid>();
            var parameters = new ArchiveItSystemUsageParameters
            {
                ArchivingDate = DateTime.UtcNow,
                ReferenceName = A<string>(),
                Note = A<string>()
            };
            
            var systemUsage = new ItSystemUsage { Id = usageId, OrganizationId = organizationId };
            var organization = new Organization { Id = organizationId, Uuid = organizationUuid };
            var createdArchive = new ItSystemArchive
            {
                OrganizationUuid = organizationUuid,
                ArchivingDate = parameters.ArchivingDate,
                ReferenceName = parameters.ReferenceName,
                Note = parameters.Note
            };
            
            _identityResolver.Setup(x => x.ResolveDbId<ItSystemUsage>(usageUuid)).Returns(usageId);
            _usageRepository.Setup(x => x.GetSystemUsage(usageId)).Returns(systemUsage);
            _organizationRepository.Setup(x => x.GetByKey(organizationId)).Returns(organization);
            _archiveRepository.Setup(x => x.Insert(It.IsAny<ItSystemArchive>())).Returns(createdArchive);

            var result = _sut.Create(usageUuid, parameters);

            Assert.False(result.Failed);
            Assert.NotNull(result.Value);
            Assert.Equal(organizationUuid, result.Value.OrganizationUuid);
            _archiveRepository.Verify(x => x.Insert(It.IsAny<ItSystemArchive>()), Times.Once);
        }
    }
}
