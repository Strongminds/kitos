﻿using System;
using System.Linq;
using Core.Abstractions.Extensions;
using Core.Abstractions.Types;
using Core.DomainModel.ItProject;
using Core.DomainServices.Extensions;


namespace Core.DomainServices.Repositories.Project
{
    public class ItProjectRepository : IItProjectRepository
    {
        private readonly IGenericRepository<ItProject> _repository;

        public ItProjectRepository(IGenericRepository<ItProject> repository)
        {
            _repository = repository;
        }

        public ItProject GetById(int id)
        {
            return _repository.AsQueryable().ById(id);
        }

        public Maybe<ItProject> GetProject(Guid uuid)
        {
            return _repository.AsQueryable().ByUuid(uuid).FromNullable();
        }

        public IQueryable<ItProject> GetProjectsInOrganization(int organizationId)
        {
            return _repository.AsQueryable().ByOrganizationId(organizationId);
        }

        public IQueryable<ItProject> GetProjects()
        {
            return _repository.AsQueryable();
        }
    }
}
