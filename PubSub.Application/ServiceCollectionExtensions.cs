using PubSub.Application.StartupTasks;
using RabbitMQ.Client;

namespace PubSub.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStartupTask<T>(this IServiceCollection services)
        where T : class, IStartupTask
        => services.AddTransient<IStartupTask, T>();

    public async static Task<IServiceCollection> AddRabbitMQ(this IServiceCollection services)
    {
        var connectionFactory = new ConnectionFactory { HostName = "localhost" };
        var connection = await connectionFactory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        services.AddScoped<IPublisher, RabbitMQPublisher>();
        services.AddSingleton(_ => channel);
        services.AddSingleton<ISubscribeLoopHostedService, RabbitMQSubscribeLoopHostedService>();
        services.AddSingleton<IMessageBusTopicManager, RabbitMQMessageBusTopicManager>();
        return services;
    }
}