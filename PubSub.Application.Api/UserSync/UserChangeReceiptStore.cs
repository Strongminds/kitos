using Microsoft.EntityFrameworkCore;
using PubSub.Core.DomainModel.UserSync;
using PubSub.Infrastructure.DataAccess;

namespace PubSub.Application.Api.UserSync;

public sealed class UserChangeReceiptStore(PubSubContext db)
{
    public async Task<UserChangeReceipt> Reserve(UserDeletionEvent message, CancellationToken cancellationToken)
    {
        var hash = UserChangeContract.Hash(message);
        var existing = await db.UserChangeReceipts.SingleOrDefaultAsync(x => x.ExternalMessageId == message.ExternalMessageId, cancellationToken);
        if (existing != null) return Compare(existing, hash);
        var receipt = new UserChangeReceipt { ExternalMessageId = message.ExternalMessageId, EventHash = hash };
        db.UserChangeReceipts.Add(receipt);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            db.Entry(receipt).State = EntityState.Detached;
            existing = await db.UserChangeReceipts.SingleOrDefaultAsync(x => x.ExternalMessageId == message.ExternalMessageId, cancellationToken);
            if (existing == null) throw;
            return Compare(existing, hash);
        }
        return receipt;
    }

    public async Task Accept(UserChangeReceipt receipt, CancellationToken cancellationToken)
    {
        receipt.AcceptedAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static UserChangeReceipt Compare(UserChangeReceipt receipt, string hash) =>
        receipt.EventHash == hash ? receipt : throw new UserChangeConflictException();
}
