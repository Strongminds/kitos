using RabbitMQ.Client;

namespace PubSub.Application.Services.RabbitMQConnection
{
    public interface IRabbitMQConnectionManager: IDisposable
    {
        Task<IConnection> GetConnectionAsync();
    }
}
