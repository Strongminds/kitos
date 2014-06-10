﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Core.DomainServices
{
    public interface IGenericRepository<TModel> : IDisposable
        where TModel : class
    {
        IEnumerable<TModel> Get(
            Expression<Func<TModel, bool>> filter = null,
            Func<IQueryable<TModel>, IOrderedQueryable<TModel>> orderBy = null,
            string includeProperties = "");
        TModel GetByKey(params object[] key);
        TModel Insert(TModel entity);
        void DeleteByKey(params object[] key);
        void Update(TModel entity);
        void Save();
        void Patch(TModel item);
        IQueryable<TModel> AsQueryable();
    }
}