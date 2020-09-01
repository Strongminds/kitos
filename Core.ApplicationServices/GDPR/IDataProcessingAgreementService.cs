﻿using System.Linq;
using Core.DomainModel.GDPR;
using Core.DomainModel.Result;
using Infrastructure.Services.Types;

namespace Core.ApplicationServices.GDPR
{
    public interface IDataProcessingAgreementService
    {
        Result<DataProcessingAgreement, OperationError> Create(int organizationId, string name);
        Maybe<OperationError> Delete(int id);
        Result<DataProcessingAgreement, OperationError> Get(int id);
        Result<IQueryable<DataProcessingAgreement>, OperationError> GetByOrganizationId(int organizationId);
    }
}
