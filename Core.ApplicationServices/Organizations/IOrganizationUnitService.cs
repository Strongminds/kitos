﻿using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Organizations;
using Core.DomainModel.Organization;
using System;

namespace Core.ApplicationServices.Organizations
{
    public interface IOrganizationUnitService
    {
        Result<UnitAccessRights, OperationError> GetAccessRights(Guid organizationUuid, Guid unitUuid);
        Result<OrganizationUnitRegistrationDetails, OperationError> GetRegistrations(Guid organizationUuid, Guid unitUuid);
        Maybe<OperationError> Delete(Guid organizationUuid, Guid unitUuid);
        Maybe<OperationError> DeleteRegistrations(Guid organizationUuid, Guid unitUuid, OrganizationUnitRegistrationChangeParameters parameters);
        Maybe<OperationError> DeleteRegistrations(Guid organizationUuid, Guid unitUuid);
        Maybe<OperationError> TransferRegistrations(Guid organizationUuid, Guid unitUuid, Guid targetUnitUuid, OrganizationUnitRegistrationChangeParameters parameters);
    }
}
