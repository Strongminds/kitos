using System.Text;
using RabbitMQ.Client;

namespace Tools.Beskedfordeler;

public sealed class TokenSaslMechanism(string token) : IAuthMechanismFactory, IAuthMechanism
{
    public string Name => "EXTERNAL";

    public IAuthMechanism GetInstance() => this;

    public Task<byte[]> HandleChallengeAsync(byte[]? challenge, ConnectionConfig config,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Encoding.UTF8.GetBytes(token));
    }
}
