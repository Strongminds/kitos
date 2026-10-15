using System;
using System.Collections.Generic;
using Infrastructure.Services.DataAccess;

namespace Infrastructure.DataAccess.Services
{
    public class WithinAmbienTransaction : IDatabaseTransaction
    {
        private readonly IDatabaseTransaction? _parent;
        private readonly Action? _onDisposed;
        private readonly List<Action> _afterCommit = new();
        private bool _completed;

        public WithinAmbienTransaction(IDatabaseTransaction? parent = null, Action? onDisposed = null)
        {
            _parent = parent;
            _onDisposed = onDisposed;
        }

        public void AfterCommit(Action callback)
        {
            if (_completed)
                throw new InvalidOperationException("The transaction scope has already completed.");
            if (_parent == null)
                throw new InvalidOperationException("After-commit callbacks require a transaction owned by ITransactionManager.");
            _afterCommit.Add(callback ?? throw new ArgumentNullException(nameof(callback)));
        }

        public void Dispose()
        {
            _completed = true;
            _afterCommit.Clear();
            _onDisposed?.Invoke();
        }

        public void Commit()
        {
            foreach (var callback in _afterCommit)
                _parent?.AfterCommit(callback);
            _afterCommit.Clear();
            _completed = true;
        }

        public void Rollback()
        {
            _afterCommit.Clear();
            _completed = true;
        }
    }
}
