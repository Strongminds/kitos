﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.DomainModel.Organization;
using Core.DomainServices;

namespace Tests.Unit.Core.ApplicationServices.KLE
{
    public class GenericRepositoryTaskRefStub : IGenericRepository<TaskRef>
    {
        private readonly Dictionary<string, TaskRef> _taskRefs = new Dictionary<string, TaskRef>();

        public IEnumerable<TaskRef> Get(Expression<Func<TaskRef, bool>> filter = null, Func<IQueryable<TaskRef>, IOrderedQueryable<TaskRef>> orderBy = null, string includeProperties = "")
        {
            return filter != null ? _taskRefs.Values.Where(filter.Compile()) : _taskRefs.Values;
        }

        public IEnumerable<TaskRef> GetWithReferencePreload<TProperty>(Expression<Func<TaskRef, TProperty>> includeExpression)
        {
            return _taskRefs.Values;
        }

        public TaskRef Insert(TaskRef entity)
        {
            // Copy by reference: Test has lifetime responsibility for TaskRefs
            _taskRefs.Add(entity.TaskKey, entity); 
            return entity;
        }

        public void Delete(TaskRef entity)
        {
            _taskRefs.Remove(entity.TaskKey);
        }

        public TaskRef GetByKey(params object[] key) { throw new NotImplementedException(); }

        public IQueryable<TaskRef> AsQueryable() { throw new NotImplementedException(); }

        public IEnumerable<TaskRef> SQL(string sql) { throw new NotImplementedException(); }

        public TaskRef Create() { throw new NotImplementedException(); }

        public void Dispose() { }
        public void DeleteWithReferencePreload(TaskRef entity) { }
        public void DeleteByKeyWithReferencePreload(params object[] key) { }
        public void DeleteByKey(params object[] key) { }
        public void Update(TaskRef entity) { }
        public void Save() { }
        public void Patch(TaskRef item) { }
    }
}
