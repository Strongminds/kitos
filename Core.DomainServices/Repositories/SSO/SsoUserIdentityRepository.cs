﻿using System;
using System.Linq;
using Core.DomainModel;
using Core.DomainModel.Result;
using Core.DomainModel.SSO;
using Infrastructure.Services.Types;

namespace Core.DomainServices.Repositories.SSO
{
    public class SsoUserIdentityRepository : ISsoUserIdentityRepository
    {
        private readonly IGenericRepository<SsoUserIdentity> _repository;

        public SsoUserIdentityRepository(IGenericRepository<SsoUserIdentity> repository)
        {
            _repository = repository;
        }

        public Maybe<SsoUserIdentity> GetByExternalUuid(Guid externalId)
        {
            return _repository
                .AsQueryable()
                .Where(identity => identity.ExternalUuid == externalId)
                .FirstOrDefault();
        }

        public Result<SsoUserIdentity, OperationError> AddNew(User user, Guid externalId)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var existing = GetByExternalUuid(externalId);
            if (existing.HasValue)
            {
                return new OperationError("Existing mapping already exists for UUID:{externalId}", OperationFailure.Conflict);
            }
            var identity = new SsoUserIdentity(externalId, user);
            identity = _repository.Insert(identity);
            _repository.Save();
            return identity;
        }
    }
}
