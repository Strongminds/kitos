﻿using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainModel.Organization.DomainEvents;
using Core.DomainServices;
using Core.DomainServices.Extensions;

namespace Core.ApplicationServices.Organizations
{
    public class OrganizationRightsService : IOrganizationRightsService
    {
        private readonly IAuthorizationContext _authorizationContext;
        private readonly IGenericRepository<OrganizationRight> _organizationRightRepository;
        private readonly IOrganizationalUserContext _userContext;
        private readonly IDomainEvents _domainEvents;

        public OrganizationRightsService(
            IAuthorizationContext authorizationContext,
            IGenericRepository<OrganizationRight> organizationRightRepository,
            IOrganizationalUserContext userContext,
            IDomainEvents domainEvents)
        {
            _authorizationContext = authorizationContext;
            _organizationRightRepository = organizationRightRepository;
            _userContext = userContext;
            _domainEvents = domainEvents;
        }

        public Result<OrganizationRight, OperationFailure> AssignRole(int organizationId, int userId, OrganizationRole roleId)
        {
            var right = new OrganizationRight
            {
                OrganizationId = organizationId,
                Role = roleId,
                UserId = userId
            };

            if (!_authorizationContext.AllowCreate<OrganizationRight>(organizationId, right))
            {
                return OperationFailure.Forbidden;
            }

            right = _organizationRightRepository.Insert(right);
            _domainEvents.Raise(new AccessRightsChanged(userId));
            _organizationRightRepository.Save();
            return right;
        }

        public Result<OrganizationRight, OperationFailure> RemoveRole(int organizationId, int userId, OrganizationRole roleId)
        {
            var right = _organizationRightRepository
                .AsQueryable()
                .ByOrganizationId(organizationId)
                .Where(r => r.Role == roleId && r.UserId == userId)
                .FirstOrDefault();

            return RemoveRight(right);
        }

        public Result<OrganizationRight, OperationFailure> RemoveRole(int rightId)
        {
            var right = _organizationRightRepository.GetByKey(rightId);

            return RemoveRight(right);
        }

        private Result<OrganizationRight, OperationFailure> RemoveRight(OrganizationRight right)
        {
            if (right == null)
            {
                return OperationFailure.NotFound;
            }

            if (!_authorizationContext.AllowDelete(right))
            {
                return OperationFailure.Forbidden;
            }

            if (right.Role == OrganizationRole.GlobalAdmin && right.UserId == _userContext.UserId)
            {
                //Only other global admins should do this. Otherwise the system could end up without a global admin
                return OperationFailure.Conflict;
            }

            _organizationRightRepository.DeleteByKey(right.Id);
            _domainEvents.Raise(new AccessRightsChanged(right.UserId));
            _organizationRightRepository.Save();

            return right;
        }
    }
}
