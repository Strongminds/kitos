using System;

namespace Infrastructure.Services.DataAccess
{
    public interface IDatabaseTransaction : IDisposable
    {
        void AfterCommit(Action callback);
        void Commit();
        void Rollback();
    }
}
