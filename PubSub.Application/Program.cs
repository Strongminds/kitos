using PubSub.Application;
using PubSub.Application.Common;
using PubSub.Application.StartupTasks;
using PubSub.Application.Subscribe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await builder.Services.AddRabbitMQ();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ISubscribeLoopHostedService>());
builder.Services.AddStartupTask<StartSubscribeLoopStartupTask>();
builder.Services.AddSingleton<IMessageSerializer, UTF8MessageSerializer>();
builder.Services.AddSingleton<ISubscriptionManager, InMemorySubscriptionManager>();

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
