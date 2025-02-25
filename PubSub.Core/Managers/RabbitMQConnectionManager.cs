using RabbitMQ.Client;

namespace PubSub.Core.Managers
{
    public class RabbitMQConnectionManager : IConnectionManager
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection? _connection;

        public RabbitMQConnectionManager()
        {
            _connectionFactory = new ConnectionFactory{HostName = "localhost"};
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = await _connectionFactory.CreateConnectionAsync();
            }
            return _connection;
        }
    }
}
