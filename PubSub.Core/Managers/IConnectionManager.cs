using RabbitMQ.Client;

namespace PubSub.Core.Managers
{
    public interface IConnectionManager
    {
        Task<IConnection> GetConnectionAsync();
    }
}
