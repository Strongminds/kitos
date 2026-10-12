using Microsoft.EntityFrameworkCore;
using PubSub.Infrastructure.DataAccess;

namespace PubSub.Application.Api.UserSync;

/// <summary>Offline, restartable transfer. All old hosts and ingress must be stopped first.</summary>
public sealed class UserChangeCutover(PubSubContext db, UserChangeReceiptStore receipts, IUserChangePublisher publisher)
{
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var migrated = 0;
        while (true)
        {
            var rows = await db.UserChangeDeliveries.Where(x => x.RabbitMqMigratedAt == null)
                .OrderBy(x => x.Uuid).Take(100).ToListAsync(cancellationToken);
            if (rows.Count == 0) return migrated;
            foreach (var row in rows)
            {
                var message = UserChangeContract.Read(System.Text.Encoding.UTF8.GetBytes(row.Payload));
                if (message.ExternalMessageId != row.ExternalMessageId)
                    throw new InvalidOperationException("Legacy message identity mismatch; cutover stopped.");
                var receipt = await receipts.Reserve(message, cancellationToken);
                if (row.DeliveredAt == null)
                    await publisher.Publish(UserChangeContract.Serialize(message), message.ExternalMessageId, 0, row.DeadLettered, cancellationToken);
                // Receipt acceptance and row checkpoint commit together. A crash before this commit can duplicate
                // broker messages, but cannot strand an old row. Never delete or change its historical status.
                row.RabbitMqMigratedAt = DateTime.UtcNow;
                await receipts.Accept(receipt, cancellationToken);
                migrated++;
            }
            db.ChangeTracker.Clear();
        }
    }
}
