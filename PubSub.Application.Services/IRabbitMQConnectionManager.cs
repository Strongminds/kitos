using RabbitMQ.Client;

namespace PubSub.Application.Services
{
    public interface IRabbitMQConnectionManager: IDisposable
    {
        Task<IConnection> GetConnectionAsync();
    }
}
