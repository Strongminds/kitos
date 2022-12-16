﻿using System;
using System.Linq;
using Core.Abstractions.Extensions;
using Core.Abstractions.Types;
using Core.DomainModel.GDPR.Read;
using Core.DomainServices.Extensions;
using Core.DomainServices.Queries.DataProcessingRegistrations;


namespace Core.DomainServices.Repositories.GDPR
{
    public class DataProcessingRegistrationReadModelRepository : IDataProcessingRegistrationReadModelRepository
    {
        private readonly IGenericRepository<DataProcessingRegistrationReadModel> _repository;

        public DataProcessingRegistrationReadModelRepository(IGenericRepository<DataProcessingRegistrationReadModel> repository)
        {
            _repository = repository;
        }

        public DataProcessingRegistrationReadModel Add(DataProcessingRegistrationReadModel newModel)
        {
            var existing = GetBySourceId(newModel.SourceEntityId);

            if (existing.HasValue)
                throw new InvalidOperationException("Only one read model per entity is allowed");

            var inserted = _repository.Insert(newModel);
            _repository.Save();
            return inserted;
        }

        public Maybe<DataProcessingRegistrationReadModel> GetBySourceId(int sourceId)
        {
            return _repository.AsQueryable().FirstOrDefault(x => x.SourceEntityId == sourceId);
        }

        public void Update(DataProcessingRegistrationReadModel updatedModel)
        {
            _repository.Save();
        }

        public void DeleteBySourceId(int sourceId)
        {
            var readModel = GetBySourceId(sourceId);
            if (readModel.HasValue)
            {
                Delete(readModel.Value);
            }
        }

        public IQueryable<DataProcessingRegistrationReadModel> GetByOrganizationId(int organizationId)
        {
            return _repository.AsQueryable().ByOrganizationId(organizationId);
        }

        public void Delete(DataProcessingRegistrationReadModel readModel)
        {
            if (readModel == null) throw new ArgumentNullException(nameof(readModel));

            _repository.DeleteWithReferencePreload(readModel);
            _repository.Save();
        }

        public IQueryable<DataProcessingRegistrationReadModel> GetByUserId(int userId)
        {
            //Gets all read models that have dependencies on the user
            return _repository
                .AsQueryable()
                .Where(x => x.RoleAssignments.Any(assignment => assignment.UserId == userId || x.LastChangedById == userId))
                .Distinct();
        }

        public IQueryable<DataProcessingRegistrationReadModel> GetReadModelsMustUpdateToChangeActiveState()
        {

            var now = DateTime.Now;
            var expiringReadModelIds = _repository
                .AsQueryable()
                .Transform(new QueryReadModelsWhichShouldExpire(now).Apply)
                .Select(x => x.Id);

            var activatingReadModelIds = _repository
                .AsQueryable()
                .Transform(new QueryReadModelsWhichShouldBecomeActive(now).Apply)
                .Select(x => x.Id);

            var idsOfReadModelsWhichMustUpdate = expiringReadModelIds.Concat(activatingReadModelIds).Distinct().ToList();

            return _repository.AsQueryable().ByIds(idsOfReadModelsWhichMustUpdate);
        }
    }
}
