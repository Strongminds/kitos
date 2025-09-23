using System;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel.Organization;

namespace Core.ApplicationServices.Organizations.Write
{
    public interface IOrganizationSupplierWriteService
    {
        Result<IEnumerable<OrganizationSupplier>, OperationError> GetSuppliersForOrganization(Guid organizationUuid);
        Result<OrganizationSupplier, OperationError> AddSupplierToOrganization(Guid organizationUuid, Guid supplierUuid);
        Maybe<OperationError> RemoveSupplierFromOrganization(Guid organizationUuid, Guid supplierUuid);
    }
}
