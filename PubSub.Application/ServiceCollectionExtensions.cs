using PubSub.Core.Managers;
using PubSub.Core.Services.Publish;
using PubSub.Core.Services.Subscribe;
using RabbitMQ.Client;

namespace PubSub.Application;

public static class ServiceCollectionExtensions
{
    public static async Task<IServiceCollection> AddRabbitMQ(this IServiceCollection services, string hostName)
    {
        var connectionFactory = new ConnectionFactory { HostName = hostName };
      //  var connection = await connectionFactory.CreateConnectionAsync(); //same as below
      //  var channel = await connection.CreateChannelAsync(); //todo not required here anymore?
      //  services.AddSingleton(_ => channel);
        services.AddSingleton<IConnectionFactory>(_ => connectionFactory);
        services.AddSingleton<ISubscriberService, RabbitMQSubscriberService>();
        services.AddSingleton<IConnectionManager, RabbitMQConnectionManager>();
        services.AddSingleton<IPublisherService, RabbitMQPublisherService>();

        return services;
    }
}