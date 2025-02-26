using PubSub.Application;
using PubSub.Core.Services.Notifier;
using PubSub.Core.Services.Serializer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var rabbitMQHostName = "localhost";
await builder.Services.AddRabbitMQ(rabbitMQHostName);
builder.Services.AddSingleton<IMessageSerializer, UTF8MessageSerializer>();
builder.Services.AddSingleton<ISubscriberNotifierService, HttpSubscriberNotifierService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
