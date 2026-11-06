using Core.DomainModel.Users;

namespace Core.DomainServices.Users;

public interface IExternalUserChangeRepository : IGenericRepository<ExternalUserChange>
{
    ExternalUserChange? FindByMessageId(string messageId);
    // Returns the existing record on a concurrent duplicate insertion.
    ExternalUserChange InsertAndSave(ExternalUserChange change);
}
