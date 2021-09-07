﻿using System;
using System.Linq;
using Core.Abstractions.Types;
using Core.DomainModel.Organization;
using Core.DomainServices.Queries;


namespace Core.DomainServices.Repositories.TaskRefs
{
    public interface ITaskRefRepository
    {
        IQueryable<TaskRef> Query(params IDomainQuery<TaskRef>[] conditions);
        Maybe<TaskRef> GetTaskRef(int id);
        Maybe<TaskRef> GetTaskRef(string key);
        Maybe<TaskRef> GetTaskRef(Guid uuid);
    }
}
