﻿using System.Data;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.Result;
using Core.DomainModel;
using Core.DomainServices;
using Core.DomainServices.Repositories.Reference;
using Core.DomainServices.Repositories.System;
using Infrastructure.Services.DataAccess;

namespace Core.ApplicationServices.System
{
    public class ReferenceService : IReferenceService
    {
        private readonly IReferenceRepository _referenceRepository;
        private readonly IItSystemRepository _itSystemRepository;
        private readonly IAuthorizationContext _authorizationContext;
        private readonly ITransactionManager _transactionManager;

        public ReferenceService(
            IReferenceRepository referenceRepository, 
            IItSystemRepository itSystemRepository, 
            IAuthorizationContext authorizationContext,
            ITransactionManager transactionManager)
        {
            _referenceRepository = referenceRepository;
            _itSystemRepository = itSystemRepository;
            _authorizationContext = authorizationContext;
            _transactionManager = transactionManager;
        }

        public OperationResult Delete(int systemId)
        {
            var system = _itSystemRepository.GetSystem(systemId);
            if (system == null)
            {
                return OperationResult.NotFound;
            }

            if (! _authorizationContext.AllowModify(system))
            {
                return OperationResult.Forbidden;
            }

            var referenceIds = system.ExternalReferences;

            if (referenceIds.Count == 0)
            {
                return OperationResult.Ok;
            }

            using (var transaction = _transactionManager.Begin(IsolationLevel.Serializable))
            {
                foreach (var reference in referenceIds)
                {
                    if (!_authorizationContext.AllowDelete(reference))
                    {
                        transaction.Rollback();
                        return OperationResult.Forbidden;
                    }
                    _referenceRepository.Delete(reference);
                }
                transaction.Commit();
                return OperationResult.Ok;
            }
            
        }

    }
}
