using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage;
using Infrastructure.Services.DataAccess;

namespace Infrastructure.DataAccess.Services
{
    public class DatabaseTransaction : IDatabaseTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private readonly List<Action> _afterCommit = new();
        private readonly Action? _onDisposed;
        private bool _completed;

        public DatabaseTransaction(IDbContextTransaction transaction, Action? onDisposed = null)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _onDisposed = onDisposed;
        }

        public void AfterCommit(Action callback)
        {
            if (_completed)
                throw new InvalidOperationException("The transaction has already completed.");
            _afterCommit.Add(callback ?? throw new ArgumentNullException(nameof(callback)));
        }

        public void Commit()
        {
            _transaction.Commit();
            _completed = true;
            var callbacks = _afterCommit.ToArray();
            _afterCommit.Clear();
            foreach (var callback in callbacks)
                callback();
        }

        public void Rollback()
        {
            _transaction.Rollback();
            _completed = true;
            _afterCommit.Clear();
        }

        public void Dispose()
        {
            _completed = true;
            _afterCommit.Clear();
            try
            {
                _transaction.Dispose();
            }
            finally
            {
                _onDisposed?.Invoke();
            }
        }
    }
}
