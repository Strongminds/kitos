using System;
using System.Data;
using System.IO;
using Core.Abstractions.Types;
using Core.ApplicationServices;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Contract.Write;
using Core.ApplicationServices.SystemUsage;
using Core.DomainModel;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Generic;
using Core.DomainServices.Repositories.Organization;
using Infrastructure.Services.Cryptography;
using Infrastructure.Services.DataAccess;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.ApplicationServices
{
    public class ExcelServiceTest : WithAutoFixture
    {
        private readonly ExcelService _sut;
        private readonly Mock<IItSystemUsageService> _itSystemUsageServiceMock;
        private readonly Mock<IExcelHandler> _excelHandlerMock;

        public ExcelServiceTest()
        {
            _itSystemUsageServiceMock = new Mock<IItSystemUsageService>();
            _excelHandlerMock = new Mock<IExcelHandler>();

            _excelHandlerMock
                .Setup(x => x.Create(It.IsAny<DataSet>(), It.IsAny<Stream>()))
                .Returns<DataSet, Stream>((_, stream) => stream);

            _sut = new ExcelService(
                Mock.Of<IGenericRepository<OrganizationUnit>>(),
                Mock.Of<IGenericRepository<User>>(),
                Mock.Of<IGenericRepository<ItContract>>(),
                Mock.Of<IGenericRepository<OrganizationRight>>(),
                _excelHandlerMock.Object,
                Mock.Of<ICryptoService>(),
                Mock.Of<ITransactionManager>(),
                Mock.Of<IItContractWriteService>(),
                Mock.Of<IOrganizationRepository>(),
                Mock.Of<IAuthorizationContext>(),
                Mock.Of<IEntityIdentityResolver>(),
                _itSystemUsageServiceMock.Object);
        }

        [Fact]
        public void ExportItSystemUsage_Returns_Ok_With_Stream_And_Filename_Containing_System_Name()
        {
            //Arrange
            var systemUsageUuid = A<Guid>();
            var systemName = A<string>();
            var usage = CreateUsageWithSystemName(systemName);

            ExpectGetItSystemUsageByUuidAndAuthorizeReadReturns(systemUsageUuid, usage);

            //Act
            var result = _sut.ExportItSystemUsage(new MemoryStream(), systemUsageUuid);

            //Assert
            Assert.True(result.Ok);
            Assert.NotNull(result.Value.stream);
            Assert.Contains(systemName, result.Value.fileName);
            Assert.StartsWith("OS2KITOS IT System - ", result.Value.fileName);
            Assert.EndsWith(".xlsx", result.Value.fileName);
        }

        [Fact]
        public void ExportItSystemUsage_Uses_Uuid_As_Filename_Fallback_When_ItSystem_Is_Null()
        {
            //Arrange
            var systemUsageUuid = A<Guid>();
            var usage = new ItSystemUsage { ItSystem = null };

            ExpectGetItSystemUsageByUuidAndAuthorizeReadReturns(systemUsageUuid, usage);

            //Act
            var result = _sut.ExportItSystemUsage(new MemoryStream(), systemUsageUuid);

            //Assert
            Assert.True(result.Ok);
            Assert.Contains(systemUsageUuid.ToString(), result.Value.fileName);
        }

        [Fact]
        public void ExportItSystemUsage_Returns_NotFound_When_Usage_Does_Not_Exist()
        {
            //Arrange
            var systemUsageUuid = A<Guid>();
            var expectedError = new OperationError(OperationFailure.NotFound);

            _itSystemUsageServiceMock
                .Setup(x => x.GetItSystemUsageByUuidAndAuthorizeRead(systemUsageUuid))
                .Returns(expectedError);

            //Act
            var result = _sut.ExportItSystemUsage(new MemoryStream(), systemUsageUuid);

            //Assert
            Assert.False(result.Ok);
            Assert.Equal(OperationFailure.NotFound, result.Error.FailureType);
        }

        [Fact]
        public void ExportItSystemUsage_Returns_Forbidden_When_User_Lacks_Read_Access()
        {
            //Arrange
            var systemUsageUuid = A<Guid>();
            var expectedError = new OperationError(OperationFailure.Forbidden);

            _itSystemUsageServiceMock
                .Setup(x => x.GetItSystemUsageByUuidAndAuthorizeRead(systemUsageUuid))
                .Returns(expectedError);

            //Act
            var result = _sut.ExportItSystemUsage(new MemoryStream(), systemUsageUuid);

            //Assert
            Assert.False(result.Ok);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        [Fact]
        public void ExportItSystemUsage_Calls_ExcelHandler_Create_With_DataSet()
        {
            //Arrange
            var systemUsageUuid = A<Guid>();
            var usage = CreateUsageWithSystemName(A<string>());
            ExpectGetItSystemUsageByUuidAndAuthorizeReadReturns(systemUsageUuid, usage);

            //Act
            _sut.ExportItSystemUsage(new MemoryStream(), systemUsageUuid);

            //Assert
            _excelHandlerMock.Verify(x => x.Create(It.Is<DataSet>(ds => ds.Tables.Count == 1), It.IsAny<Stream>()), Times.Once);
        }

        private void ExpectGetItSystemUsageByUuidAndAuthorizeReadReturns(Guid uuid, ItSystemUsage usage)
        {
            _itSystemUsageServiceMock
                .Setup(x => x.GetItSystemUsageByUuidAndAuthorizeRead(uuid))
                .Returns(Result<ItSystemUsage, OperationError>.Success(usage));
        }

        private static ItSystemUsage CreateUsageWithSystemName(string systemName)
        {
            return new ItSystemUsage
            {
                ItSystem = new ItSystem { Name = systemName },
                Organization = new Organization { Name = "Test Organisation" }
            };
        }
    }
}
