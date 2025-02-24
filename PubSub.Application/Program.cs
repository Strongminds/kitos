using PubSub.Application;
using PubSub.Application.StartupTasks;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

var connectionFactory = new ConnectionFactory { HostName = "localhost" };
var connection = await connectionFactory.CreateConnectionAsync();
var channel = await connection.CreateChannelAsync();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPublisher, RabbitMQPublisher>();
builder.Services.AddSingleton(_ => channel);
builder.Services.AddSingleton<IMessageSerializer, UTF8MessageSerializer>();
builder.Services.AddSingleton<ISubscribeLoopHostedService, RabbitMQSubscribeLoopHostedService>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ISubscribeLoopHostedService>());
builder.Services.AddStartupTask<StartSubscribeLoopStartupTask>();

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
