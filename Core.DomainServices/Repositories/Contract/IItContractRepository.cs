﻿using System;
using System.Linq;
using Core.DomainModel.ItContract;
using Infrastructure.Services.Types;

namespace Core.DomainServices.Repositories.Contract
{
    public interface IItContractRepository
    {
        IQueryable<ItContract> GetBySystemUsageAssociation(int systemUsageId);
        ItContract GetById(int contractId);
        IQueryable<ItContract> GetByOrganizationId(int organizationId);
        void DeleteContract(ItContract contract);
        void Update(ItContract contract);
        IQueryable<ItContract> GetContracts();
        Maybe<ItContract> GetContract(Guid uuid);
    }
}
