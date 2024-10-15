﻿using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Organizations;
using Core.ApplicationServices.Model.Organizations.Write;
using Core.ApplicationServices.Model.Organizations.Write.MasterDataRoles;
using Core.DomainModel.Organization;
using System;

namespace Core.ApplicationServices.Organizations.Write
{
    public interface IOrganizationWriteService
    {
        Result<Organization, OperationError> PatchMasterData(Guid organizationUuid,
            OrganizationMasterDataUpdateParameters parameters);

        Result<OrganizationMasterDataRoles, OperationError> PatchOrganizationMasterDataRoles(Guid organizationUuid, OrganizationMasterDataRolesUpdateParameters updateParameters);
        
        Result<OrganizationMasterDataRoles, OperationError> GetOrCreateOrganizationMasterDataRoles(Guid organizationUuid);

        Result<Organization, OperationError> PatchOrganization(Guid organizationUuid,
            OrganizationUpdateParameters parameters);
    }
}
