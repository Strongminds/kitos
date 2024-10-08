﻿using System;
using System.Linq;
using Core.Abstractions.Types;


namespace Core.DomainServices.Repositories.Organization
{
    public interface IOrganizationRepository
    {
        IQueryable<DomainModel.Organization.Organization> GetAll();
        Maybe<DomainModel.Organization.Organization> GetById(int id);
        Maybe<DomainModel.Organization.Organization> GetByCvr(string cvrNumber);
        Maybe<DomainModel.Organization.Organization> GetByUuid(Guid uuid);
        void Update(DomainModel.Organization.Organization organization);
    }
}
