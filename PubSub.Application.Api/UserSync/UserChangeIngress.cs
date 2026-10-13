using UserSync.Transport;
using PubSub.Core.DomainModel.UserSync;

namespace PubSub.Application.Api.UserSync;

public sealed class UserChangeIngress(UserChangeReceiptStore receipts, IUserChangePublisher publisher)
{
    public async Task<UserChangeReceipt> Enqueue(UserDeletionEvent message, CancellationToken cancellationToken)
    {
        message = UserChangeContract.Normalize(message);
        var receipt = await receipts.Reserve(message, cancellationToken);
        if (receipt.AcceptedAt != null) return receipt;
        // No database transaction spans the broker call. Ambiguous outcomes remain pending.
        await publisher.Publish(UserChangeContract.Serialize(message), message.ExternalMessageId, 0, false, cancellationToken);
        await receipts.Accept(receipt, cancellationToken);
        return receipt;
    }
}
