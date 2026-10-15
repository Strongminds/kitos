using System;
using Infrastructure.Services.DataAccess;

namespace Infrastructure.DataAccess.Services
{
    public class TransactionManager : ITransactionManager, IDisposable
    {
        private readonly KitosContext _context;
        private IDatabaseTransaction? _currentScope;

        public TransactionManager(KitosContext context)
        {
            _context = context;
        }

        public IDatabaseTransaction Begin()
        {
            var currentTransaction = _context.Database.CurrentTransaction;
            var parent = _currentScope;
            Action restoreParent = () => _currentScope = parent;
            if (currentTransaction != null)
            {
                _currentScope = new WithinAmbienTransaction(parent, restoreParent);
            }
            else
            {
                _currentScope = new DatabaseTransaction(_context.Database.BeginTransaction(), restoreParent);
            }
            return _currentScope;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
