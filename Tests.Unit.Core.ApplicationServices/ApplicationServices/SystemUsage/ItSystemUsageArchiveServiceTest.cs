using Core.Abstractions.Types;
using Core.ApplicationServices.Model.SystemUsage;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices.Generic;
using Core.DomainServices.Repositories.SystemUsage;
using Moq;
using System;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.SystemUsage
{
    public class ItSystemUsageArchiveServiceTest: WithAutoFixture
    {
        private Mock<IItSystemUsageRepository> _repository;
        private Mock<IEntityIdentityResolver> _identityResolver;
        private ItSystemUsageArchiveService _sut;

        public ItSystemUsageArchiveServiceTest() {
            _repository = new Mock<IItSystemUsageRepository>();
            _identityResolver = new Mock<IEntityIdentityResolver>();
            _sut = new ItSystemUsageArchiveService(_repository.Object, _identityResolver.Object);
        }

        [Fact]
        public void Create_ReturnsNotFound_IfUsageNotFoundInRepository()
        {
            var usageUuid = A<Guid>();
            var usageId = A<int>();
            _identityResolver.Setup(x => x.ResolveDbId<ItSystemUsage>(usageUuid)).Returns(usageId);
            _repository.Setup(x => x.GetSystemUsage(usageId)).Returns((ItSystemUsage)null);

            var result = _sut.Create(usageUuid);

            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.NotFound, result.Error);
        }
    }
}
