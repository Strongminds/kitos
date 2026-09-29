using System;
using System.Threading;
using System.Threading.Tasks;
using Core.DomainModel.Users;

namespace Core.DomainServices.Users;

public interface IExternalUserChangeStore
{
    Task<ExternalUserChange?> FindByMessageId(string messageId, CancellationToken cancellationToken);
    // Returns the existing record on a concurrent duplicate insertion.
    Task<ExternalUserChange> Insert(ExternalUserChange change, CancellationToken cancellationToken);
}
