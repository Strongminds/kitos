using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.Users;
using Core.DomainServices.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.Repositories;

public class ExternalUserChangeReadStore(KitosContext db) : IExternalUserChangeReadStore
{
    public IQueryable<ExternalUserChange> Query(int organizationId) =>
        db.Set<ExternalUserChange>().AsNoTracking().Where(x => x.OrganizationId == organizationId)
            .Include(x => x.User).Include(x => x.ResolvedByUser);

    public int Count(IQueryable<ExternalUserChange> query) => query.Count();

    public List<ExternalUserChange> List(IQueryable<ExternalUserChange> query) => query.ToList();
}
