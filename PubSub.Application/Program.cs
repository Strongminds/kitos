using PubSub.Application;
using PubSub.Application.StartupTasks;
using PubSub.Application.Subscribe;
using PubSub.Core.Managers;
using PubSub.Core.Services;
using PubSub.Core.Services.Publish;
using PubSub.Core.Services.Serializer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var rabbitMQHostName = "localhost";
await builder.Services.AddRabbitMQ(rabbitMQHostName);
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ISubscribeLoopHostedService>());
builder.Services.AddStartupTask<StartSubscribeLoopStartupTask>();
builder.Services.AddSingleton<IMessageSerializer, UTF8MessageSerializer>();
builder.Services.AddSingleton<ISubscriptionManager, InMemorySubscriptionManager>();
builder.Services.AddHttpClient<ISubscriberNotifier, HttpSubscriberNotifier>();
builder.Services.AddSingleton<ISubscriberNotifier, HttpSubscriberNotifier>();
builder.Services.AddSingleton<ISubscriberService, RabbitMQSubscriberService>();
builder.Services.AddSingleton<IConnectionManager, RabbitMQConnectionManager>();
builder.Services.AddSingleton<ISubscriberNotifierService, HttpSubscriberNotifierService>();
builder.Services.AddSingleton<IPublisherService, RabbitMQPublisherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await StartupTaskRunner.RunAsync(app.Services.GetServices<IStartupTask>());

await app.RunAsync();
