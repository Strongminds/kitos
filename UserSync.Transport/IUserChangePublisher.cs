namespace UserSync.Transport;

public interface IUserChangePublisher
{
    Task Publish(ReadOnlyMemory<byte> body, string messageId, int attempt, bool deadLetter, CancellationToken cancellationToken);
}
