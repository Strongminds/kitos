namespace UserSync.Transport;

public enum UserChangeDeliveryResult { Accepted, Rejected, Retry }

public interface IUserChangeDelivery
{
    Task<UserChangeDeliveryResult> Deliver(UserDeletionEvent message, CancellationToken cancellationToken);
}
