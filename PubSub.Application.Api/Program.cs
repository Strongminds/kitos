using Microsoft.EntityFrameworkCore;
using PubSub.Application.Api;
using PubSub.Infrastructure.DataAccess;
using PubSub.Application.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable(Constants.Config.Environment.CurrentEnvironment) ?? Constants.Config.Environment.Production;
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environment}.json")
    .AddEnvironmentVariables();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    if (context.HostingEnvironment.IsDevelopment())
    {
        // In development/Docker, listen on HTTP (port configured via ASPNETCORE_URLS)
        return;
    }

    options.ListenAnyIP(443, listenOptions =>
    {
        var certPassword = Environment.GetEnvironmentVariable(Constants.Config.Certificate.CertPassword);
        listenOptions.UseHttps(Constants.Config.Certificate.CertFilePath, certPassword);
    });
});

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PubSubContext>();
    var allowAutoMigrate = app.Environment.IsDevelopment() || IsAutoMigrateEnabled();

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

static bool IsAutoMigrateEnabled()
{
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
