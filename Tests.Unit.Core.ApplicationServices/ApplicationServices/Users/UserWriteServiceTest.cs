﻿using System;
using Core.ApplicationServices.Organizations;
using Core.ApplicationServices;
using Core.ApplicationServices.Model.Users.Write;
using Core.ApplicationServices.Users.Write;
using Core.DomainModel;
using Infrastructure.Services.DataAccess;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;
using Core.DomainModel.Organization;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Authorization.Permissions;
using Core.ApplicationServices.Rights;
using Core.DomainServices.Generic;
using Core.ApplicationServices.Model.Users;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystem;

namespace Tests.Unit.Core.ApplicationServices.Users
{
    public class UserWriteServiceTest : WithAutoFixture
    {
        private readonly UserWriteService _sut;

        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IOrganizationRightsService> _organizationRightsServiceMock;
        private readonly Mock<ITransactionManager> _transactionManagerMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly Mock<IOrganizationService> _organizationServiceMock;
        private readonly Mock<IEntityIdentityResolver> _entityIdentityResolverMock;
        private readonly Mock<IUserRightsService> _userRightsServiceMock;

        public UserWriteServiceTest()
        {
            _userServiceMock = new Mock<IUserService>();
            _organizationRightsServiceMock = new Mock<IOrganizationRightsService>();
            _transactionManagerMock = new Mock<ITransactionManager>();
            _authorizationContextMock = new Mock<IAuthorizationContext>();
            _organizationServiceMock = new Mock<IOrganizationService>();
            _entityIdentityResolverMock = new Mock<IEntityIdentityResolver>();
            _userRightsServiceMock = new Mock<IUserRightsService>();


            _sut = new UserWriteService(_userServiceMock.Object, 
                _organizationRightsServiceMock.Object, 
                _transactionManagerMock.Object,
                _authorizationContextMock.Object,
                _organizationServiceMock.Object,
                _entityIdentityResolverMock.Object,
                _userRightsServiceMock.Object);
        }

        [Fact]
        public void Can_Create_User()
        {
            //Arrange
            var createParams = SetupUserParameters();
            var orgUuid = A<Guid>();
            var orgId = A<int>();
            var org = new Organization { Id = orgId};
            var transaction = ExpectTransactionBegins();

            ExpectIsEmailInUseReturns(createParams.User.Email, false);
            ExpectHasGlobalAdminPermissionReturns(true);
            ExpectHasStakeHolderAccessReturns(true);
            ExpectGetOrganizationReturns(orgUuid, org);
            ExpectPermissionsReturn(org, true);
            ExpectAddUserReturns(createParams.User, createParams.SendMailOnCreation, orgId);
            foreach (var organizationRole in createParams.Roles)
            {
                ExpectAddRoleReturns(organizationRole, orgId, createParams.User.Id, Result<OrganizationRight, OperationFailure>.Success(It.IsAny<OrganizationRight>()));
            }

            //Act
            var result = _sut.Create(orgUuid, createParams);

            //Assert
            Assert.True(result.Ok);
            transaction.Verify(x => x.Commit(), Times.Once);
        }

        [Fact]
        public void Create_Fails_If_Add_Role_Fails()
        {
            //Arrange
            var createParams = SetupUserParameters();
            var orgUuid = A<Guid>();
            var orgId = A<int>();
            var org = new Organization { Id = orgId };
            var transaction = ExpectTransactionBegins();
            var error = A<OperationFailure>();

            ExpectIsEmailInUseReturns(createParams.User.Email, false);
            ExpectHasGlobalAdminPermissionReturns(true);
            ExpectHasStakeHolderAccessReturns(true);
            ExpectGetOrganizationReturns(orgUuid, org);
            ExpectPermissionsReturn(org, true);
            ExpectAddUserReturns(createParams.User, createParams.SendMailOnCreation, orgId);
            foreach (var organizationRole in createParams.Roles)
            {
                ExpectAddRoleReturns(organizationRole, orgId, createParams.User.Id, error);
            }

            //Act
            var result = _sut.Create(orgUuid, createParams);

            //Assert
            Assert.True(result.Failed);
            Assert.Equal(error, result.Error.FailureType);
            Assert.True(result.Error.Message.HasValue);
            Assert.True(result.Error.Message.Value.Contains("Failed to assign role"));
            transaction.Verify(x => x.Rollback(), Times.Once);
        }

        [Theory]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        public void Create_Returns_Forbidden_If_Permission_Is_Missing(bool accessPermissions, bool globalAdminPermission, bool stakeHolderPermission)
        {
            //Arrange
            var createParams = SetupUserParameters();
            var orgUuid = A<Guid>();
            var org = new Organization { Id = A<int>() };

            ExpectIsEmailInUseReturns(createParams.User.Email, false);
            ExpectHasGlobalAdminPermissionReturns(globalAdminPermission);
            ExpectHasStakeHolderAccessReturns(stakeHolderPermission);
            ExpectGetOrganizationReturns(orgUuid, org);
            ExpectPermissionsReturn(org, accessPermissions);

            //Act
            var result = _sut.Create(orgUuid, createParams);

            //Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        [Fact]
        public void Create_Fails_If_GetOrganization_Fails()
        {
            //Arrange
            var createParams = SetupUserParameters();
            var orgUuid = A<Guid>();
            var error = A<OperationError>();

            ExpectIsEmailInUseReturns(createParams.User.Email, false);
            ExpectHasGlobalAdminPermissionReturns(true);
            ExpectHasStakeHolderAccessReturns(true);
            ExpectGetOrganizationReturns(orgUuid, error);

            //Act
            var result = _sut.Create(orgUuid, createParams);

            //Assert
            Assert.True(result.Failed);
            Assert.Equal(error.FailureType, result.Error.FailureType);
        }


        [Fact]
        public void Can_Send_Notification()
        {
            //Arrange
            var orgUuid = A<Guid>();
            var orgId = A<int>();
            var userUuid = A<Guid>();
            var user = SetupUser();
            user.Uuid = userUuid;
            ExpectResolveIdReturns(orgUuid, Maybe<int>.Some(orgId));
            ExpectGetUserInOrganizationReturns(orgUuid, userUuid, user);
            //Act
            var result = _sut.SendNotification(orgUuid, userUuid);

            _userServiceMock.Verify(x => x.IssueAdvisMail(user, false, orgId), Times.Once);
            Assert.True(result.IsNone);
        }

        [Fact]
        public void Create_Fails_If_Email_Already_Exists()
        {
            //Arrange
            var createParams = SetupUserParameters();
            var orgUuid = A<Guid>();

            ExpectIsEmailInUseReturns(createParams.User.Email, true);

            //Act
            var result = _sut.Create(orgUuid, createParams);

            //Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.BadInput, result.Error.FailureType);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void Can_Get_User_Permissions(bool canCreate, bool canEdit, bool canDelete)
        {
            //Arrange
            var orgUuid = A<Guid>();
            var orgId = A<int>();
            var org = new Organization { Id = orgId };

            ExpectGetOrganizationReturns(orgUuid, org);
            ExpectCreatePermissionReturns(orgId, canCreate);
            ExpectModifyPermissionReturns(org, canEdit);
            ExpectDeletePermissionReturns(orgId, canDelete);

            //Act
            var result = _sut.GetCollectionPermissions(orgUuid);

            //Assert
            Assert.True(result.Ok);
            var permissions = result.Value;
            Assert.Equal(canCreate, permissions.Create);
            Assert.Equal(canEdit, permissions.Edit);
            Assert.Equal(canDelete, permissions.Delete);
        }

        [Fact]
        public void Can_Update_User()
        {
            //Arrange
            var user = SetupUser();
            var organization = new Organization {Id = A<int>(), Uuid = A<Guid>()};
            var defaultUnit = new OrganizationUnit {Id = A<int>()};
            var updateParameters = A<UpdateUserParameters>();
            ExpectGetUserByUuid(user.Uuid, user);
            ExpectModifyPermissionsForUserReturns(user, true);
            ExpectGetOrganizationReturns(organization.Uuid, organization);
            ExpectResolveIdReturns(organization.Uuid, organization.Id);
            ExpectHasStakeHolderAccessReturns(true);
            ExpectAssignRolesReturn(updateParameters.Roles.NewValue, user, organization);
            ExpectRemoveRolesReturn(user.GetRolesInOrganization(organization.Uuid), user, organization);
            _organizationServiceMock.Setup(x => x.GetDefaultUnit(organization, user)).Returns(defaultUnit);
            var transaction = ExpectTransactionBegins();

            //Act
            var updatedUserResult = _sut.Update(organization.Uuid, user.Uuid, updateParameters);

            //Assert
            Assert.True(updatedUserResult.Ok);
            var updatedUser = updatedUserResult.Value;
            Assert.Equal(updateParameters.Email.NewValue, updatedUser.Email);
            Assert.Equal(updateParameters.FirstName.NewValue, updatedUser.Name);
            Assert.Equal(updateParameters.LastName.NewValue, updatedUser.LastName);
            Assert.Equal(updateParameters.PhoneNumber.NewValue, updatedUser.PhoneNumber);
            Assert.Equal(updateParameters.HasApiAccess.NewValue, updatedUser.HasApiAccess);
            Assert.Equal(updateParameters.HasStakeHolderAccess.NewValue, updatedUser.HasStakeHolderAccess);
            Assert.Equal(updateParameters.DefaultUserStartPreference.NewValue, updatedUser.DefaultUserStartPreference);
            transaction.Verify(x => x.Commit(), Times.AtLeastOnce);
        }

        [Fact]
        public void Can_Not_Update_User_If_Email_Is_Already_In_Use() 
        {
            //Arrange
            var user = SetupUser();
            var organization = new Organization { Id = A<int>(), Uuid = A<Guid>() };
            var updateParameters = A<UpdateUserParameters>();
            ExpectResolveIdReturns(organization.Uuid, A<int>());
            ExpectGetOrganizationReturns(organization.Uuid, organization);
            ExpectGetUserByUuid(user.Uuid, user);
            ExpectModifyPermissionsForUserReturns(user, true);
            ExpectHasStakeHolderAccessReturns(true);
            ExpectIsEmailInUseReturns(updateParameters.Email.NewValue, true);
            ExpectAssignRolesReturn(updateParameters.Roles.NewValue, user, organization);
            ExpectRemoveRolesReturn(user.GetRolesInOrganization(organization.Uuid), user, organization);
            ExpectTransactionBegins();

            //Act
            var updateResult = _sut.Update(organization.Uuid, user.Uuid, updateParameters);

            //Assert
            Assert.True(updateResult.Failed);

        }

        [Fact]
        public void Can_Copy_Roles()
        {
            //Arrange
            var fromUser = SetupUser();
            var toUser = SetupUser();
            var org = new Organization { Id = A<int>(), Uuid = A<Guid>() };
            var updateParameters = A<UserRightsChangeParameters>();
            ExpectGetUserInOrganizationReturns(org.Uuid, fromUser.Uuid, fromUser);
            ExpectGetUserInOrganizationReturns(org.Uuid, toUser.Uuid, toUser);
            ExpectGetOrganizationReturns(org.Uuid, org);
            ExpectModifyPermissionsForUserReturns(toUser, true);
            ExpectGetUserRightsReturnsNothing(toUser, org);

            _userRightsServiceMock.Setup(x => x.CopyRights(fromUser.Id, toUser.Id, org.Id, It.IsAny<UserRightsChangeParameters>()))
                .Returns(Maybe<OperationError>.None);
            var transaction = ExpectTransactionBegins();

            //Act
            var result = _sut.CopyUserRights(org.Uuid, fromUser.Uuid, toUser.Uuid, updateParameters);

            //Assert
            Assert.True(result.IsNone);
            _userRightsServiceMock.Verify(x => x.CopyRights(fromUser.Id, toUser.Id, org.Id, It.IsAny<UserRightsChangeParameters>()));

            transaction.Verify(x => x.Commit(), Times.AtLeastOnce);
        }

        private void ExpectGetUserRightsReturnsNothing(User user, Organization org)
        {
            var emptyAssignments = new UserRightsAssignments(new List<OrganizationRole>(),
                new List<DataProcessingRegistrationRight>(), new List<ItSystemRight>(), new List<ItContractRight>(),
                new List<OrganizationUnitRight>());
            _userRightsServiceMock.Setup(x => x.GetUserRights(user.Id, org.Id)).Returns(emptyAssignments);
        }

        private void ExpectAssignRolesReturn(IEnumerable<OrganizationRole> roles, User user, Organization org)
        {
            foreach (var role in roles)
            {
                var expectedRight = new OrganizationRight { Role = role, UserId = user.Id, Organization = org, OrganizationId = org.Id };
                _organizationRightsServiceMock.Setup(x => x.AssignRole(org.Id, user.Id, role)).Returns(expectedRight);
            }
        }

        private void ExpectRemoveRolesReturn(IEnumerable<OrganizationRole> roles, User user, Organization org)
        {
            foreach (var role in roles)
            {
                var expectedRight = new OrganizationRight { Role = role, UserId = user.Id, OrganizationId = org.Id };
                _organizationRightsServiceMock.Setup(x => x.RemoveRole(org.Id, user.Id, role)).Returns(expectedRight);
            }
        }

        private void ExpectResolveIdReturns(Guid orgUuid, Maybe<int> result)
        {
            _entityIdentityResolverMock.Setup(x => x.ResolveDbId<Organization>(orgUuid)).Returns(result);
        }

        private void ExpectGetUserInOrganizationReturns(Guid organizationUuid, Guid userUuid, Result<User, OperationError> result)
        {
            _userServiceMock.Setup(x => x.GetUserInOrganization(organizationUuid, userUuid)).Returns(result);
        }

        private void ExpectGetUserByUuid(Guid userUuid, Result<User, OperationError> result)
        {
            _userServiceMock.Setup(x => x.GetUserByUuid(userUuid)).Returns(result);
        }


        private void ExpectModifyPermissionsForUserReturns(User user, bool result)
        {
            _authorizationContextMock.Setup(x => x.AllowModify(user)).Returns(result);
        }

        private void ExpectAddUserReturns(User user, bool sendMailOnCreation, int orgId)
        {
            _userServiceMock.Setup(x => x.AddUser(user, sendMailOnCreation, orgId)).Returns(user);
        }
        
        private void ExpectAddRoleReturns(OrganizationRole role, int organizationId, int userId, Result<OrganizationRight, OperationFailure> result)
        {
            _organizationRightsServiceMock.Setup(x => x.AssignRole(organizationId, userId, role)).Returns(result);
        }

        private void ExpectGetOrganizationReturns(Guid organizationUuid, Result<Organization, OperationError> result)
        {
            _organizationServiceMock.Setup(x => x.GetOrganization(organizationUuid, null)).Returns(result);
        }

        private void ExpectCreatePermissionReturns(int organizationId, bool result)
        {
            _authorizationContextMock.Setup(x => x.AllowCreate<User>(organizationId)).Returns(result);
        }

        private void ExpectModifyPermissionReturns(Organization organization, bool result)
        {
            _authorizationContextMock.Setup(x => x.AllowModify(organization)).Returns(result);
        }

        private void ExpectDeletePermissionReturns(int organizationId, bool result)
        {
            _authorizationContextMock.Setup(x => x.HasPermission(It.Is<DeleteAnyUserPermission>(parameter => parameter.OptionalOrganizationScopeId.Value == organizationId))).Returns(result);
        }

        private void ExpectPermissionsReturn(Organization organization, bool result)
        {
            ExpectCreatePermissionReturns(organization.Id, result);
            ExpectModifyPermissionReturns(organization, result);
            ExpectDeletePermissionReturns(organization.Id, result);
        }

        private void ExpectIsEmailInUseReturns(string email, bool result)
        {
            _userServiceMock.Setup(x => x.IsEmailInUse(email)).Returns(result);
        }

        private void ExpectHasGlobalAdminPermissionReturns(bool result)
        {
            _authorizationContextMock.Setup(x =>
                x.HasPermission(
                    It.Is<AdministerGlobalPermission>(perm => perm.Permission == GlobalPermission.GlobalAdmin)))
                .Returns(result);
        }

        private void ExpectHasStakeHolderAccessReturns(bool result)
        {
            _authorizationContextMock.Setup(x =>
                x.HasPermission(
                    It.Is<AdministerGlobalPermission>(perm => perm.Permission == GlobalPermission.StakeHolderAccess)))
                .Returns(result);
        }

        private Mock<IDatabaseTransaction> ExpectTransactionBegins()
        {
            var transactionMock = new Mock<IDatabaseTransaction>();
            _transactionManagerMock.Setup(x => x.Begin()).Returns(transactionMock.Object);
            return transactionMock;
        }

        private CreateUserParameters SetupUserParameters()
        {
            return new CreateUserParameters
            {
                Roles = A<IEnumerable<OrganizationRole>>(),
                SendMailOnCreation = A<bool>(),
                User = SetupUser()
            };
        }

        private User SetupUser()
        {
            return new User
            {
                Id = A<int>(),
                Uuid = A<Guid>(),
                Email = A<string>(),
                Name = A<string>(),
                LastName = A<string>(),
                PhoneNumber = A<string>(),
                DefaultUserStartPreference = "index",
                HasApiAccess = A<bool>(),
                HasStakeHolderAccess = true,
                IsGlobalAdmin = true
            };
        }
    }
}
