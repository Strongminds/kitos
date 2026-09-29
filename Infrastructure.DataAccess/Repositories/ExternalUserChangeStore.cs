using System.Threading;
using System.Threading.Tasks;
using Core.DomainModel.Users;
using Core.DomainServices.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.Repositories;

public class ExternalUserChangeStore(KitosContext context) : IExternalUserChangeStore
{
    public Task<ExternalUserChange?> FindByMessageId(string messageId, CancellationToken cancellationToken) =>
        context.Set<ExternalUserChange>().SingleOrDefaultAsync(x => x.ExternalMessageId == messageId, cancellationToken);

    public async Task<ExternalUserChange> Insert(ExternalUserChange change, CancellationToken cancellationToken)
    {
        context.Set<ExternalUserChange>().Add(change);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return change;
        }
        catch (DbUpdateException)
        {
            context.Entry(change).State = EntityState.Detached;
            // Only suppress a failure when the competing transaction actually persisted this ID.
            var existing = await FindByMessageId(change.ExternalMessageId, cancellationToken);
            if (existing != null) return existing;
            throw;
        }
    }
}
