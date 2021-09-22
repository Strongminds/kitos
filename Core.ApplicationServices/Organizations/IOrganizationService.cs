﻿using System;
using System.Linq;
using Core.Abstractions.Types;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainServices.Authorization;
using Core.DomainServices.Queries;

namespace Core.ApplicationServices.Organizations
{
    public interface IOrganizationService
    {
        //returns the default org unit for that user inside that organization
        //or null if none has been chosen
        OrganizationUnit GetDefaultUnit(Organization organization, User user);

        void SetDefaultOrgUnit(User user, int orgId, int orgUnitId);

        Result<Organization, OperationFailure> RemoveUser(int organizationId, int userId);

        bool CanChangeOrganizationType(Organization organization, OrganizationTypeKeys organizationType);

        Result<Organization, OperationFailure> CreateNewOrganization(Organization newOrg);

        public Result<Organization, OperationError> GetOrganization(Guid organizationUuid, OrganizationDataReadAccessLevel? withMinimumAccessLevel = null);
        public Result<IQueryable<Organization>, OperationError> GetAllOrganizations();
        public IQueryable<Organization> SearchAccessibleOrganizations(params IDomainQuery<Organization>[] conditions);

        public Result<IQueryable<OrganizationUnit>, OperationError> GetOrganizationUnits(Guid organizationUuid, params IDomainQuery<OrganizationUnit>[] criteria);
        public Result<OrganizationUnit, OperationError> GetOrganizationUnit(Guid organizationUnitUuid);
    }
}
