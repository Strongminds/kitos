﻿using System;
using Core.Abstractions.Types;
using Core.DomainModel;
using Core.DomainModel.Shared;
using Core.DomainServices.Repositories.Contract;
using Core.DomainServices.Repositories.GDPR;
using Core.DomainServices.Repositories.SystemUsage;


namespace Core.DomainServices.Advice
{
    public class AdviceRootResolution : IAdviceRootResolution
    {
        private readonly IItSystemUsageRepository _itSystemUsageRepository;
        private readonly IItContractRepository _itContractRepository;
        private readonly IDataProcessingRegistrationRepository _dataProcessingRegistrationRepository;

        public AdviceRootResolution(
            IItSystemUsageRepository itSystemUsageRepository,
            IItContractRepository itContractRepository,
            IDataProcessingRegistrationRepository dataProcessingRegistrationRepository
            )
        {
            _itSystemUsageRepository = itSystemUsageRepository;
            _itContractRepository = itContractRepository;
            _dataProcessingRegistrationRepository = dataProcessingRegistrationRepository;
        }

        public Maybe<IEntityWithAdvices> Resolve(DomainModel.Advice.Advice advice)
        {
            if (advice?.Type != null && advice.RelationId != null)
            {
                var adviceRelationId = advice.RelationId.Value;

                switch (advice.Type)
                {
                    case RelatedEntityType.itContract:
                        return _itContractRepository.GetById(adviceRelationId);
                    case RelatedEntityType.itSystemUsage:
                        return _itSystemUsageRepository.GetSystemUsage(adviceRelationId);
                    case RelatedEntityType.dataProcessingRegistration:
                        return _dataProcessingRegistrationRepository.GetById(adviceRelationId).GetValueOrDefault();
                    case RelatedEntityType.itInterface: //Intended fallthrough
                    default:
                        throw new NotSupportedException("Unsupported object type:" + advice.Type);
                }
            }

            return Maybe<IEntityWithAdvices>.None;
        }
    }
}
