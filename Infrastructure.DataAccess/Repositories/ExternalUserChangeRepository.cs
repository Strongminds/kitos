using System.Threading;
using System.Threading.Tasks;
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

    public Task<ExternalUserChange?> FindByMessageId(string messageId, CancellationToken cancellationToken) =>
        AsQueryable().SingleOrDefaultAsync(x => x.ExternalMessageId == messageId, cancellationToken);

    public async Task<ExternalUserChange> Insert(ExternalUserChange change, CancellationToken cancellationToken)
    {
        base.Insert(change);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return change;
        }
        catch (DbUpdateException)
        {
            _context.Entry(change).State = EntityState.Detached;
            // Only suppress a failure when the competing transaction actually persisted this ID.
            var existing = await FindByMessageId(change.ExternalMessageId, cancellationToken);
            if (existing != null) return existing;
            throw;
        }
    }
}
