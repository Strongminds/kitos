﻿using System;
using System.Collections.Generic;
using System.Web.ModelBinding;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Authorization.Permissions;
using Core.ApplicationServices.Model.Users;
using Core.ApplicationServices.Model.Users.Write;
using Core.ApplicationServices.Organizations;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Infrastructure.Services.DataAccess;

namespace Core.ApplicationServices.Users.Write
{
    public class UserWriteService : IUserWriteService
    {
        private readonly IUserService _userService;
        private readonly IOrganizationRightsService _organizationRightsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IAuthorizationContext _authorizationContext;
        private readonly IOrganizationService _organizationService;

        public UserWriteService(IUserService userService,
            IOrganizationRightsService organizationRightsService,
            ITransactionManager transactionManager, 
            IAuthorizationContext authorizationContext,
            IOrganizationService organizationService)
        {
            _userService = userService;
            _organizationRightsService = organizationRightsService;
            _transactionManager = transactionManager;
            _authorizationContext = authorizationContext;
            _organizationService = organizationService;
        }

        public Result<User, OperationError> Create(Guid organizationUuid, CreateUserParameters parameters)
        {
            return ValidateUserCanBeCreated(parameters)
                .Match(
                error => error, 
                () => 
                    ValidateIfCanCreateUser(organizationUuid)
                    .Bind(organization =>
                        {
                            using var transaction = _transactionManager.Begin();


                            var user = _userService.AddUser(parameters.User, parameters.SendMailOnCreation, organization.Id);

                            var roleAssignmentError = AssignUserAdministrativeRoles(organization.Id, user.Id, parameters.Roles);

                            if (roleAssignmentError.HasValue)
                            {
                                transaction.Rollback();
                                return roleAssignmentError.Value;
                            }

                            transaction.Commit();
                            return Result<User, OperationError>.Success(user);
                        }
                    )
                );
        }

        public Maybe<OperationError> SendNotification(Guid organizationUuid, Guid userUuid)
        {
            var orgIdResult = ResolveOrganizationUuidToId(organizationUuid);
            if (orgIdResult.Failed)
            {
                return orgIdResult.Error;
            }

            var user = _userService.GetUserInOrganization(organizationUuid, userUuid);
            if (user.Failed)
            {
                return user.Error;
            }
            _userService.IssueAdvisMail(user.Value, false, orgIdResult.Value);
            return Maybe<OperationError>.None;
        }
        
        public Result<UserCollectionPermissionsResult, OperationError> GetCollectionPermissions(
            Guid organizationUuid)
        {
            return _organizationService.GetOrganization(organizationUuid)
                .Select(org => UserCollectionPermissionsResult.FromOrganization(org, _authorizationContext));
        }

        private Result<UserCollectionPermissionsResult, OperationError> GetCollectionPermissions(Organization organization)
        {
            return UserCollectionPermissionsResult.FromOrganization(organization, _authorizationContext);
        }

        private Result<Organization, OperationError> ValidateIfCanCreateUser(Guid organizationUuid)
        {
            return _organizationService.GetOrganization(organizationUuid)
                .Bind(organization =>
                {
                    var permissionsResult = GetCollectionPermissions(organization);
                    if (permissionsResult.Failed)
                        return permissionsResult.Error;
                    if(permissionsResult.Value.Create == false)
                        return new OperationError(
                            $"User is not allowed to create users for organization with uuid: {organizationUuid}",
                            OperationFailure.Forbidden);

                    return Result<Organization, OperationError>.Success(organization);
                });
        }
        
        private Maybe<OperationError> AssignUserAdministrativeRoles(int organizationId, int userId,
            IEnumerable<OrganizationRole> roles)
        {
            foreach (var role in roles)
            {
                var result = _organizationRightsService.AssignRole(organizationId, userId, role);
                if (result.Failed)
                    return new OperationError($"Failed to assign role: {role}", result.Error);
            }

            return Maybe<OperationError>.None;
        }

        private Result<int, OperationError> ResolveOrganizationUuidToId(Guid organizationUuid)
        {
            var orgIdResult
                = _entityIdentityResolver.ResolveDbId<Organization>(organizationUuid);
            if (orgIdResult.IsNone)
            {
                return new OperationError($"Organization with uuid {organizationUuid} was not found",
                    OperationFailure.NotFound);
            }

            return orgIdResult.Value;

        private Maybe<OperationError> ValidateUserCanBeCreated(CreateUserParameters parameters)
        {
            var user = parameters.User;
            if (user.Email != null && _userService.IsEmailInUse(user.Email))
            {
                return new OperationError("Email is already in use.", OperationFailure.BadInput);
            }

            // user is being created as global admin
            if (user.IsGlobalAdmin)
            {
                // only other global admins can create global admin users
                if (!_authorizationContext.HasPermission(new AdministerGlobalPermission(GlobalPermission.GlobalAdmin)))
                {
                    return new OperationError("You don't have permission to create a global admin user.", OperationFailure.Forbidden);
                }
            }

            if (user.HasStakeHolderAccess)
            {
                // only global admins can create stakeholder access
                if (!_authorizationContext.HasPermission(new AdministerGlobalPermission(GlobalPermission.StakeHolderAccess)))
                {
                    return new OperationError("You don't have permission to issue stakeholder access.", OperationFailure.Forbidden);
                }
            }

            return Maybe<OperationError>.None;

        }
    }
}