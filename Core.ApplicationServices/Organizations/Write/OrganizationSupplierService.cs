using Core.Abstractions.Types;
using Core.DomainModel.Organization;
using Core.DomainModel.SupplierAssociatedFields;
using Core.DomainServices;
using Core.DomainServices.Generic;
using Core.DomainServices.Queries;
using Core.DomainServices.Queries.Organization;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Suppliers;
using Infrastructure.Services.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.ApplicationServices.Organizations.Write
{
    public class OrganizationSupplierService : IOrganizationSupplierService
    {
        private readonly IGenericRepository<OrganizationSupplier> _organizationSupplierRepository;
        private readonly IOrganizationService _organizationService;
        private readonly IEntityIdentityResolver _entityIdentityResolver;
        private readonly ITransactionManager _transactionManager;
        private readonly IOrganizationRepository _organizationRepository;

        public OrganizationSupplierService(IGenericRepository<OrganizationSupplier> organizationSupplierRepository,
            IOrganizationService organizationService,
            IEntityIdentityResolver entityIdentityResolver, 
            ITransactionManager transactionManager,
            IOrganizationRepository organizationRepository)
        {
            _organizationSupplierRepository = organizationSupplierRepository;
            _organizationService = organizationService;
            _entityIdentityResolver = entityIdentityResolver;
            _transactionManager = transactionManager;
            _organizationRepository = organizationRepository;
        }

        public Result<IEnumerable<OrganizationSupplier>, OperationError> GetSuppliersForOrganization(
            Guid organizationUuid)
        {
            return _organizationSupplierRepository.GetWithReferencePreload(x => x.Supplier)
                .Where(x => x.Organization.Uuid == organizationUuid).ToList();
        }

        public Result<IEnumerable<Organization>, OperationError> GetAvailableSuppliers(Guid organizationUuid)
        {
            var refinements = new List<IDomainQuery<Organization>>
            {
                new QueryByIsSupplier(true),
                new QueryByIsAvailableAsSupplierForOrganization(organizationUuid)
            };

            return _organizationService
                .SearchAccessibleOrganizations(false, refinements.ToArray())
                .ToList();
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
                        _organizationSupplierRepository.Delete(supplier!);
                        _organizationSupplierRepository.Save();
                        transaction.Commit();

                        return Maybe<OperationError>.None;
                    },
                    error => error);
        }

        public Result<IEnumerable<Organization>, OperationError> GetUsingOrganizations(Guid supplierUuid)
        {
            return ResolveOrganizationId(supplierUuid)
                .Select(supplierId => _organizationSupplierRepository.AsQueryable()
                .Where(x => x.SupplierId == supplierId)
                .Select(x => x.Organization)
                .AsEnumerable());
        }

        private Result<OrganizationSupplier?, OperationError> GetByUuids(Guid organizationUuid, Guid supplierUuid)
        {
            return ResolveIds(organizationUuid, supplierUuid)
                .Select(tuple => _organizationSupplierRepository.AsQueryable().FirstOrDefault(x =>
                    x.OrganizationId == tuple.organizationId && x.SupplierId == tuple.supplierId));
        }

        private Result<(int organizationId, int supplierId), OperationError> ResolveIds(Guid organizationUuid, Guid supplierUuid)
        {
            return ResolveOrganizationId(organizationUuid)
                .Bind(orgId => ResolveOrganizationId(supplierUuid)
                    .Select(supId => (orgId, supId)));
        }

        private Result<int, OperationError> ResolveOrganizationId(Guid organizationUuid)
        {
            var orgIdResult = _entityIdentityResolver.ResolveDbId<Organization>(organizationUuid);
            if (orgIdResult.IsNone)
            {
                return new OperationError($"Organization with uuid {organizationUuid} not found", OperationFailure.NotFound);
            }

            return orgIdResult.Value;
        }

        public ISet<SupplierAssociatedFieldConfiguration> GetSupplierFieldConfigurations(Guid organizationUuid)
        {
            var configurations = new HashSet<SupplierAssociatedFieldConfiguration>(
                SupplierAssociatedFields.DefaultConfiguration.Select(c => new SupplierAssociatedFieldConfiguration
                {
                    FieldKey = c.FieldKey,
                    ControlState = c.ControlState
                })
            );

            var organizationalConfigurations = _organizationRepository.GetSupplierAssociatedFieldConfigurations(organizationUuid);
            if (organizationalConfigurations is null) return configurations;
            
            foreach (var orgConfig in organizationalConfigurations.Value)
            {
                var defaultConfig = configurations.FirstOrDefault(c => c.FieldKey == orgConfig.FieldKey);
                defaultConfig?.ControlState = orgConfig.ControlState;
            }

            return configurations;
        }
    }
}
