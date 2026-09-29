using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PubSub.Core.DomainModel.UserSync;
using PubSub.Infrastructure.DataAccess;

namespace PubSub.Application.Api.UserSync;

public class UserChangeOutbox(PubSubContext db)
{
    public async Task<UserChangeDelivery> Enqueue(UserDeletionEvent message, CancellationToken cancellationToken)
    {
        if (!message.IsValid()) throw new ArgumentException("Invalid deletion event.", nameof(message));
        var payload = JsonSerializer.Serialize(new { Payload = message });
        var existing = await db.UserChangeDeliveries.SingleOrDefaultAsync(x => x.ExternalMessageId == message.ExternalMessageId, cancellationToken);
        if (existing != null) return CheckDuplicate(existing, payload);
        var delivery = new UserChangeDelivery { ExternalMessageId = message.ExternalMessageId, Payload = payload };
        db.UserChangeDeliveries.Add(delivery);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            db.Entry(delivery).State = EntityState.Detached;
            existing = await db.UserChangeDeliveries.SingleOrDefaultAsync(x => x.ExternalMessageId == message.ExternalMessageId, cancellationToken);
            if (existing == null) throw;
            return CheckDuplicate(existing, payload);
        }
        return delivery;
    }

    private static UserChangeDelivery CheckDuplicate(UserChangeDelivery existing, string payload) =>
        existing.Payload == payload ? existing : throw new InvalidOperationException("Message ID already belongs to another event.");
}
