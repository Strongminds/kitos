using System.Linq;
using Core.DomainModel.Users;
using Core.DomainServices.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.Repositories;

public class ExternalUserChangeRepository : GenericRepository<ExternalUserChange>, IExternalUserChangeRepository
{
    private readonly KitosContext _context;

    public ExternalUserChangeRepository(KitosContext context) : base(context)
    {
        _context = context;
    }

    public ExternalUserChange? FindByMessageId(string messageId) =>
        AsQueryable().SingleOrDefault(x => x.ExternalMessageId == messageId);

    public ExternalUserChange InsertAndSave(ExternalUserChange change)
    {
        base.Insert(change);
        try
        {
            Save();
            return change;
        }
        catch (DbUpdateException)
        {
            _context.Entry(change).State = EntityState.Detached;
            // Only suppress a failure when the competing transaction actually persisted this ID.
            var existing = FindByMessageId(change.ExternalMessageId);
            if (existing != null) return existing;
            throw;
        }
    }
}
