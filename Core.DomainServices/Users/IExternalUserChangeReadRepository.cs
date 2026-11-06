using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.Users;

namespace Core.DomainServices.Users;

public interface IExternalUserChangeReadRepository
{
    IQueryable<ExternalUserChange> Query(int organizationId);
    int Count(IQueryable<ExternalUserChange> query);
    List<ExternalUserChange> List(IQueryable<ExternalUserChange> query);
}
