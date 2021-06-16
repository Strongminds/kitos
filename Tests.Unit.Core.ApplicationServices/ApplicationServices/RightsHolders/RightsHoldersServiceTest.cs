﻿using System;
using System.Linq;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Interface;
using Core.ApplicationServices.RightsHolders;
using Core.ApplicationServices.System;
using Core.DomainModel.ItSystem;
using Core.DomainModel.Organization;
using Core.DomainModel.Result;
using Core.DomainServices;
using Core.DomainServices.Queries;
using Infrastructure.Services.DataAccess;
using Moq;
using Serilog;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.RightsHolders
{
    public class RightsHoldersServiceTest : WithAutoFixture
    {
        private readonly RightsHoldersService _sut;
        private readonly Mock<IOrganizationalUserContext> _userContextMock;
        private readonly Mock<IGenericRepository<Organization>> _organizationRepositoryMock;
        private readonly Mock<IItSystemService> _itSystemServiceMock;
        private readonly Mock<IItInterfaceService> _interfaceService;

        public RightsHoldersServiceTest()
        {
            _userContextMock = new Mock<IOrganizationalUserContext>();
            _organizationRepositoryMock = new Mock<IGenericRepository<Organization>>();
            _itSystemServiceMock = new Mock<IItSystemService>();
            _interfaceService = new Mock<IItInterfaceService>();
            _sut = new RightsHoldersService(_userContextMock.Object, _organizationRepositoryMock.Object, _interfaceService.Object, _itSystemServiceMock.Object,Mock.Of<ITransactionManager>(),Mock.Of<ILogger>());
        }

        //TODO: Test all added methods!!!

        [Fact]
        public void ResolveOrganizationsWhereAuthenticatedUserHasRightsHolderAccess_Returns_Only_Organizations_Where_User_Has_RightsHolderAccessRole()
        {
            //Arrange
            Organization expectedOrg1 = new() { Id = A<int>() };
            Organization expectedOrg2 = new() { Id = expectedOrg1.Id + 1 };
            Organization noMatchOrg = new() { Id = expectedOrg2.Id + 1 };

            _userContextMock.Setup(x => x.GetOrganizationIdsWhereHasRole(OrganizationRole.RightsHolderAccess))
                .Returns(new[] { expectedOrg1.Id, expectedOrg2.Id });
            _organizationRepositoryMock.Setup(x => x.AsQueryable())
                .Returns(new EnumerableQuery<Organization>(new[] { noMatchOrg, expectedOrg1, expectedOrg2 }));

            //Act
            var organizations = _sut.ResolveOrganizationsWhereAuthenticatedUserHasRightsHolderAccess().ToList();

            //Assert
            Assert.Equal(2, organizations.Count);
            Assert.Same(expectedOrg1, organizations.First());
            Assert.Same(expectedOrg2, organizations.Last());
        }

        [Fact]
        public void GetAvailableSystems_Returns_Forbidden_If_User_Does_Not_Have_RightsHoldersAccess()
        {
            //Arrange
            ExpectUserHasRightsHolderAccessReturns(false);

            //Act
            var result = _sut.GetSystemsWhereAuthenticatedUserHasRightsHolderAccess();

            //Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        [Fact]
        public void Can_GetAvailableSystems_If_User_Has_RightsHoldersAccess()
        {
            //Arrange
            ExpectUserHasRightsHolderAccessReturns(true);
            var expectedResponse = Mock.Of<IQueryable<ItSystem>>();
            _userContextMock.Setup(x => x.GetOrganizationIdsWhereHasRole(OrganizationRole.RightsHolderAccess)).Returns(Many<int>());
            _itSystemServiceMock.Setup(x => x.GetAvailableSystems(It.IsAny<IDomainQuery<ItSystem>>())).Returns(expectedResponse);

            //Act
            var result = _sut.GetSystemsWhereAuthenticatedUserHasRightsHolderAccess();

            //Assert
            Assert.True(result.Ok);
            Assert.Same(expectedResponse, result.Value);
        }

        [Fact]
        public void GetSystemAsRightsHolder_Returns_Forbidden_If_User_Does_Not_Have_RightsHoldersAccess()
        {
            //Arrange
            ExpectUserHasRightsHolderAccessReturns(false);

            //Act
            var result = _sut.GetSystemAsRightsHolder(A<Guid>());

            //Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        [Fact]
        public void GetSystemAsRightsHolder_Returns_Forbidden_If_User_Does_Not_Have_RightsHoldersAccess_To_The_Retrieved_System()
        {
            //Arrange
            var systemUuid = A<Guid>();
            ExpectUserHasRightsHolderAccessReturns(true);
            var itSystem = new ItSystem()
            {
                BelongsToId = A<int>()
            };
            _itSystemServiceMock.Setup(x => x.GetSystem(systemUuid)).Returns(itSystem);
            ExpectHasSpecificAccessReturns(itSystem, false);

            //Act
            var result = _sut.GetSystemAsRightsHolder(systemUuid);

            //Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        [Fact]
        public void Can_GetSystemAsRightsHolderIf_User_Has_RightsHoldersAccess()
        {
            //Arrange
            var systemUuid = A<Guid>();
            ExpectUserHasRightsHolderAccessReturns(true);
            var itSystem = new ItSystem()
            {
                BelongsToId = A<int>()
            };
            ExpectHasSpecificAccessReturns(itSystem, true);
            _itSystemServiceMock.Setup(x => x.GetSystem(systemUuid)).Returns(itSystem);

            //Act
            var result = _sut.GetSystemAsRightsHolder(systemUuid);

            //Assert
            Assert.True(result.Ok);
            Assert.Same(itSystem, result.Value);
        }

        private void ExpectHasSpecificAccessReturns(ItSystem itSystem, bool value)
        {
            _userContextMock.Setup(x => x.HasRole(itSystem.BelongsToId.Value, OrganizationRole.RightsHolderAccess))
                .Returns(value);
        }

        private void ExpectUserHasRightsHolderAccessReturns(bool value)
        {
            _userContextMock.Setup(x => x.HasRoleInAnyOrganization(OrganizationRole.RightsHolderAccess)).Returns(value);
        }
    }
}
