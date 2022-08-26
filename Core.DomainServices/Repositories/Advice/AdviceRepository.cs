﻿using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.Shared;

namespace Core.DomainServices.Repositories.Advice
{
    public class AdviceRepository : IAdviceRepository
    {
        private readonly IGenericRepository<DomainModel.Advice.Advice> _advicesRepository;

        public AdviceRepository(IGenericRepository<DomainModel.Advice.Advice> advicesRepository)
        {
            _advicesRepository = advicesRepository;
        }

        public ISet<int> GetAllIds()
        {
            return _advicesRepository
                .AsQueryable()
                .Select(x => x.Id)
                .ToHashSet();
        }

        public IEnumerable<DomainModel.Advice.Advice> GetByRelationIdAndType(int relationId, RelatedEntityType objectType)
        {
            return _advicesRepository.Get(a => a.RelationId == relationId && a.Type == objectType);
        }
    }
}
