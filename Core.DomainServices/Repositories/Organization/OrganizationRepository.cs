using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Extensions;
using Core.Abstractions.Types;
using Core.DomainModel.SupplierAssociatedFields;
using Core.DomainServices.Extensions;


namespace Core.DomainServices.Repositories.Organization
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly IGenericRepository<DomainModel.Organization.Organization> _genericRepository;

        public OrganizationRepository(IGenericRepository<DomainModel.Organization.Organization> repository)
        {
            _genericRepository = repository;
        }

        public IQueryable<DomainModel.Organization.Organization> GetAll()
        {
            return _genericRepository.AsQueryable();
        }

        public Maybe<DomainModel.Organization.Organization> GetById(int id)
        {
            return _genericRepository.AsQueryable().ById(id);
        }

        public Maybe<DomainModel.Organization.Organization> GetByCvr(string cvrNumber)
        {
            return _genericRepository
                .AsQueryable()
                .Where(organization => organization.Cvr == cvrNumber)
                .FirstOrDefault();
        }

        public Maybe<DomainModel.Organization.Organization> GetByUuid(Guid uuid)
        {
            return _genericRepository
                .AsQueryable()
                .ByUuid(uuid);
        }

        public void Update(DomainModel.Organization.Organization organization)
        {
            _genericRepository.Update(organization);
            _genericRepository.Save();
        }

        public Maybe<IEnumerable<SupplierAssociatedFieldConfiguration>> GetSupplierAssociatedFieldConfigurations(Guid organizationUuid)
        {
            var organizationMaybe = GetByUuid(organizationUuid);
            if (organizationMaybe.IsNone) return Maybe<IEnumerable<SupplierAssociatedFieldConfiguration>>.None;
            var supplierAssociatedFieldConfigurations = organizationMaybe.Value.SupplierAssociatedFieldConfigurations;
            return supplierAssociatedFieldConfigurations.IsNullOrEmpty()
                ? Maybe<IEnumerable<SupplierAssociatedFieldConfiguration>>.None
                : Maybe<IEnumerable<SupplierAssociatedFieldConfiguration>>.Some(supplierAssociatedFieldConfigurations.AsEnumerable());
        }
    }
}
