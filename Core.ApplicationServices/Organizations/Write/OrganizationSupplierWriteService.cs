using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Generic;

namespace Core.ApplicationServices.Organizations.Write
{
    public class OrganizationSupplierWriteService : IOrganizationSupplierWriteService
    {
        private readonly IGenericRepository<OrganizationSupplier> _organizationSupplierRepository;
        private readonly IOrganizationService _organizationService;
        private readonly IEntityIdentityResolver _entityIdentityResolver;

        public OrganizationSupplierWriteService(IGenericRepository<OrganizationSupplier> organizationSupplierRepository,
            IOrganizationService organizationService,
            IEntityIdentityResolver entityIdentityResolver)
        {
            _organizationSupplierRepository = organizationSupplierRepository;
            _organizationService = organizationService;
            _entityIdentityResolver = entityIdentityResolver;
        }

        public Result<IEnumerable<OrganizationSupplier>, OperationError> GetSuppliersForOrganization(Guid organizationUuid)
        {
            return _organizationService.GetOrganization(organizationUuid)
                .Select<IEnumerable<OrganizationSupplier>>(x => x.Suppliers.ToList());
        }

        public Maybe<OperationError> AddSupplierToOrganization(Guid organizationUuid, Guid supplierUuid)
        {
            throw new NotImplementedException();
        }

        public Maybe<OperationError> RemoveSupplierFromOrganization(Guid organizationUuid, Guid supplierUuid)
        {
            throw new NotImplementedException();
        }
    }
}
