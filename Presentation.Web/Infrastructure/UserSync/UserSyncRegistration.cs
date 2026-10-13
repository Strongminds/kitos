using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using UserSync.Transport;

namespace Presentation.Web.Infrastructure.UserSync;

public static class UserSyncRegistration
{
    public static IServiceCollection AddUserSyncConsumer(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("UserSync:ConsumerEnabled")) return services;
        var broker = configuration.GetSection("UserSync:Broker");
        var host = Required(broker, "HostName");
        var tls = broker.GetValue<bool>("TlsEnabled");
        services.AddSingleton<IConnectionFactory>(new ConnectionFactory
        {
            HostName = host,
            UserName = Required(broker, "UserName"),
            Password = Required(broker, "Password"),
            Port = broker.GetValue<int?>("Port") ?? (tls ? 5671 : 5672),
            VirtualHost = broker["VirtualHost"] ?? "/",
            AutomaticRecoveryEnabled = false,
            Ssl = new SslOption { Enabled = tls, ServerName = broker["TlsServerName"] ?? host }
        });
        services.AddSingleton<UserChangeTopology>();
        services.AddSingleton<IUserChangePublisher, UserChangePublisher>();
        services.AddSingleton<IUserChangeDelivery, KitosUserChangeDelivery>();
        services.AddHostedService<UserChangeConsumer>();
        services.AddHealthChecks().AddCheck<UserSyncHealthCheck>("user-sync", tags: new[] { "user-sync" });
        return services;
    }
    private static string Required(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]!
            : throw new InvalidOperationException($"UserSync broker {key} is required.");
}
