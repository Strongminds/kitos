using RabbitMQ.Client;

namespace PubSub.Core.DomainServices.RabbitMQConnection
{
    public interface IRabbitMQConnectionManager: IDisposable
    {
        Task<IConnection> GetConnectionAsync();
    }
}
