using PubSub.Application.StartupTasks;
using RabbitMQ.Client;

namespace PubSub.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStartupTask<T>(this IServiceCollection services)
        where T : class, IStartupTask
        => services.AddTransient<IStartupTask, T>();

    public static async Task<IServiceCollection> AddRabbitMQ(this IServiceCollection services, string hostName)
    {
        var connectionFactory = new ConnectionFactory { HostName = hostName };
        var connection = await connectionFactory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        services.AddSingleton(_ => channel);
        services.AddSingleton<IMessageBusTopicManager, RabbitMQMessageBusTopicManager>();

        return services;
    }
}