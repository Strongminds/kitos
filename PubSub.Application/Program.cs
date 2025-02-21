using PubSub.Application;
using PubSub.Application.StartupTasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPublisher, RabbitMQPublisher>();
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
