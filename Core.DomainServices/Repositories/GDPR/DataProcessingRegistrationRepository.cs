﻿using System;
using System.Linq;
using Core.DomainModel.GDPR;
using Core.DomainServices.Extensions;
using Infrastructure.Services.DomainEvents;
using Infrastructure.Services.Extensions;
using Infrastructure.Services.Types;

namespace Core.DomainServices.Repositories.GDPR
{
    public class DataProcessingRegistrationRepository : IDataProcessingRegistrationRepository
    {
        private readonly IGenericRepository<DataProcessingRegistration> _repository;
        private readonly IDomainEvents _domainEvents;

        public DataProcessingRegistrationRepository(IGenericRepository<DataProcessingRegistration> repository, IDomainEvents domainEvents)
        {
            _repository = repository;
            _domainEvents = domainEvents;
        }


        public DataProcessingRegistration Add(DataProcessingRegistration newRegistration)
        {
            var registration = _repository.Insert(newRegistration);
            _repository.Save();
            Notify(registration, LifeCycleEventType.Created);
            return registration;
        }

        public bool DeleteById(int id)
        {
            var registration = _repository.GetByKey(id);
            if (registration != null)
            {
                Notify(registration, LifeCycleEventType.Deleted);
                registration.Reference.Track();
                registration.Reference = null;
                registration.ExternalReferences.Clear();
                _repository.DeleteWithReferencePreload(registration);
                _repository.Save();
                return true;
            }

            return false;
        }

        public void Update(DataProcessingRegistration dataProcessingRegistration)
        {
            Notify(dataProcessingRegistration, LifeCycleEventType.Updated);
            _repository.Save();
        }

        public Maybe<DataProcessingRegistration> GetById(int id)
        {
            return _repository.GetByKey(id);
        }

        public IQueryable<DataProcessingRegistration> GetDataProcessingRegistrationsFromOrganization(int organizationId)
        {
            return _repository.AsQueryable().ByOrganizationId(organizationId);
        }

        public IQueryable<DataProcessingRegistration> GetBySystemId(int systemId)
        {
            return _repository.AsQueryable().Where(x => x.SystemUsages.Any(usage => usage.ItSystemId == systemId));
        }

        public IQueryable<DataProcessingRegistration> GetByContractId(int contractId)
        {
            return _repository.AsQueryable().Where(x => x.AssociatedContracts.Any(contract => contract.Id == contractId));
        }

        public IQueryable<DataProcessingRegistration> Search(int organizationId, Maybe<string> exactName)
        {
            return
                _repository
                    .AsQueryable()
                    .ByOrganizationId(organizationId)
                    .Transform(previousQuery => exactName.Select(previousQuery.ByNameExact).GetValueOrFallback(previousQuery));
        }

        public IQueryable<DataProcessingRegistration> GetByDataProcessorId(int organizationId)
        {
            return _repository
                .AsQueryable()
                .Where(x =>
                    x.DataProcessors.Any(organization => organization.Id == organizationId) ||
                    x.SubDataProcessors.Any(organization => organization.Id == organizationId
                    )
                );
        }

        public IQueryable<DataProcessingRegistration> GetByBasisForTransferId(int basisForTransferId)
        {
            return _repository
                .AsQueryable()
                .Where(x => x.BasisForTransferId == basisForTransferId);
        }

        public IQueryable<DataProcessingRegistration> GetByDataResponsibleId(int dataResponsibleId)
        {
            return _repository
                .AsQueryable()
                .Where(x => x.DataResponsible_Id == dataResponsibleId);
        }

        public IQueryable<DataProcessingRegistration> GetByOversightOptionId(int oversightOptionId)
        {
            return _repository
                .AsQueryable()
                .Where(x => x.OversightOptions.Any(option => option.Id == oversightOptionId));
        }

        private void Notify(DataProcessingRegistration dataProcessingRegistration, LifeCycleEventType changeType)
        {
            switch (changeType)
            {
                case LifeCycleEventType.Created:
                    _domainEvents.Raise(new EntityCreatedEvent<DataProcessingRegistration>(dataProcessingRegistration));
                    break;
                case LifeCycleEventType.Updated:
                    _domainEvents.Raise(new EntityUpdatedEvent<DataProcessingRegistration>(dataProcessingRegistration));
                    break;
                case LifeCycleEventType.Deleted:
                    _domainEvents.Raise(new EntityDeletedEvent<DataProcessingRegistration>(dataProcessingRegistration));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
            }
        }

        
    }
}
