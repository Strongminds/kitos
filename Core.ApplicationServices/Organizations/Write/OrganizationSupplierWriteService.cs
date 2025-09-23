using Core.Abstractions.Types;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Generic;
using Infrastructure.Services.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.ApplicationServices.Organizations.Write
{
    public class OrganizationSupplierWriteService : IOrganizationSupplierWriteService
    {
        private readonly IGenericRepository<OrganizationSupplier> _organizationSupplierRepository;
        private readonly IOrganizationService _organizationService;
        private readonly IEntityIdentityResolver _entityIdentityResolver;
        private readonly IDomainEvents _domainEvents;
        private readonly ITransactionManager _transactionManager;

        public OrganizationSupplierWriteService(IGenericRepository<OrganizationSupplier> organizationSupplierRepository,
            IOrganizationService organizationService,
            IEntityIdentityResolver entityIdentityResolver, 
            IDomainEvents domainEvents, 
            ITransactionManager transactionManager)
        {
            _organizationSupplierRepository = organizationSupplierRepository;
            _organizationService = organizationService;
            _entityIdentityResolver = entityIdentityResolver;
            _domainEvents = domainEvents;
            _transactionManager = transactionManager;
        }

        public Result<IEnumerable<OrganizationSupplier>, OperationError> GetSuppliersForOrganization(
            Guid organizationUuid)
        {
            return _organizationSupplierRepository.GetWithReferencePreload(x => x.Supplier)
                .Where(x => x.Organization.Uuid == organizationUuid).ToList();
        }

        public Result<OrganizationSupplier, OperationError> AddSupplierToOrganization(Guid organizationUuid, Guid supplierUuid)
        {
            var supplierExistsError = _organizationService.GetOrganization(organizationUuid)
                .Match(x => x.CheckIfAlreadyHasSupplier(supplierUuid), error => error);
            if (supplierExistsError.HasValue)
                return supplierExistsError.Value;

            return ResolveIds(organizationUuid, supplierUuid)
                .Select(tuple => OrganizationSupplier.CreateSupplier(tuple.organizationId, tuple.supplierId))
                .Select(supplier =>
                {
                    using var transaction = _transactionManager.Begin();
                    var insertedSupplier = _organizationSupplierRepository.Insert(supplier);
                    _organizationSupplierRepository.Save();
                    _domainEvents.Raise(new EntityUpdatedEvent<Organization>(insertedSupplier.Organization));
                    _domainEvents.Raise(new EntityUpdatedEvent<Organization>(insertedSupplier.Supplier));
                    transaction.Commit();

                    return insertedSupplier;
                });
        }

        public Maybe<OperationError> RemoveSupplierFromOrganization(Guid organizationUuid, Guid supplierUuid)
        {
            return GetByUuids(organizationUuid, supplierUuid)
                .Match(supplier =>
                    {
                        using var transaction = _transactionManager.Begin();
                        _organizationSupplierRepository.Delete(supplier);
                        _organizationSupplierRepository.Save();
                        _domainEvents.Raise(new EntityUpdatedEvent<Organization>(supplier.Organization));
                        _domainEvents.Raise(new EntityUpdatedEvent<Organization>(supplier.Supplier));
                        transaction.Commit();

                        return Maybe<OperationError>.None;
                    },
                    error => error);
        }

        private Result<OrganizationSupplier, OperationError> GetByUuids(Guid organizationUuid, Guid supplierUuid)
        {
            return ResolveIds(organizationUuid, supplierUuid)
               .Select(tuple => _organizationSupplierRepository.AsQueryable().FirstOrDefault(x =>
                    x.OrganizationId == tuple.organizationId && x.SupplierId == tuple.supplierId));
        }

        private Result<(int organizationId, int supplierId), OperationError> ResolveIds(Guid organizationUuid, Guid supplierUuid)
        {
            var orgIdResult = _entityIdentityResolver.ResolveDbId<Organization>(organizationUuid);
            if (orgIdResult.IsNone)
            {
                return new OperationError($"Organization with uuid {organizationUuid} not found", OperationFailure.NotFound);
            }
            var supplierIdResult = _entityIdentityResolver.ResolveDbId<Organization>(supplierUuid);
            if (supplierIdResult.IsNone)
            {
                return new OperationError($"Supplier organization with uuid {supplierUuid} not found", OperationFailure.NotFound);
            }

            return (orgIdResult.Value, supplierIdResult.Value);
        }
    }
}
