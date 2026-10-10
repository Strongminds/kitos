using Microsoft.EntityFrameworkCore;
using PubSub.Application.Api;
using PubSub.Infrastructure.DataAccess;
using PubSub.Application.Api.Configuration;
using PubSub.Application.Api.UserSync.Beskedfordeler;

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable(Constants.Config.Environment.CurrentEnvironment) ?? Constants.Config.Environment.Production;
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environment}.json")
    .AddEnvironmentVariables();

builder.Services.AddControllers(options =>
{
    options.Conventions.Insert(0, new ApiVersioningConvention());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddPubSubServices(builder.Configuration);
builder.Services.AddScoped<PubSub.Application.Api.UserSync.UserChangeOutbox>();
builder.Services.AddHttpClient("UserSync", client => client.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHostedService<PubSub.Application.Api.UserSync.UserChangeDeliveryWorker>();
builder.AddBeskedfordeler();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PubSubContext>();
    var allowAutoMigrate = IsAutoMigrateEnabled(app.Environment);

    var pendingMigrations = context.Database.GetPendingMigrations().ToArray();
    if (pendingMigrations.Any())
    {
        if (allowAutoMigrate)
        {
            context.Database.Migrate();
        }
        else
        {
            var migrationList = string.Join(", ", pendingMigrations);
            throw new InvalidOperationException(
                $"Pending database migrations detected ({migrationList}). Apply migrations before startup or set {Constants.Config.Database.AutoMigrate}=true to opt in to automatic migrations.");
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();


static bool IsLocal(IWebHostEnvironment environment)
{
    return environment.IsEnvironment(Constants.Config.Environment.Local);
}

static bool IsAutoMigrateEnabled(IWebHostEnvironment environment)
{
    if (environment.IsDevelopment() || IsLocal(environment))
    {
        return true;
    }

    var value = Environment.GetEnvironmentVariable(Constants.Config.Database.AutoMigrate);
    if (string.IsNullOrWhiteSpace(value))
    {
        return false;
    }

    if (bool.TryParse(value, out var parsed))
    {
        return parsed;
    }

    return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
}
