using Microsoft.EntityFrameworkCore;
using PubSub.Application.Api;
using PubSub.Infrastructure.DataAccess;
using PubSub.Application.Api.Configuration;
using PubSub.Application.Api.UserSync.Beskedfordeler;
using PubSub.Application.Api.UserSync;

const string cutoverArgument = "--migrate-user-sync-outbox";
var runCutover = args.Contains(cutoverArgument, StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(x => x != cutoverArgument).ToArray());

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
builder.Services.AddScoped<UserChangeReceiptStore>();
builder.Services.AddScoped<UserChangeIngress>();
builder.Services.AddScoped<UserChangeCutover>();
builder.Services.AddSingleton<UserChangeTopology>();
builder.Services.AddSingleton<IUserChangePublisher, UserChangePublisher>();
builder.Services.AddSingleton<KitosUserChangeDelivery>();
builder.Services.AddHttpClient("UserSync", client => client.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHostedService<UserChangeConsumer>();
builder.Services.AddHealthChecks().AddCheck<UserChangeHealthCheck>("user-sync");
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

    if (runCutover)
    {
        var count = await scope.ServiceProvider.GetRequiredService<UserChangeCutover>().Run(CancellationToken.None);
        app.Logger.LogInformation("User sync cutover reconciled {Count} legacy rows; no untransferred rows remain.", count);
        return;
    }
    if (builder.Configuration.GetValue<bool>("UserSync:Enabled") &&
        await context.UserChangeDeliveries.AnyAsync(x => x.RabbitMqMigratedAt == null))
        throw new InvalidOperationException("Legacy user sync rows require offline --migrate-user-sync-outbox before enabling delivery.");
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/user-sync");

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
