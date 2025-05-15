using Microsoft.EntityFrameworkCore;
using PubSub.Application.Api;
using PubSub.Infrastructure.DataAccess;
using PubSub.Application.Api.Configuration;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.DataProtection;


[DllImport("System.Net.Security.Native", EntryPoint = "NetSecurityNative_EnsureGssInitialized")]
static extern int EnsureGssInitialized();

if (OperatingSystem.IsLinux())
{
    Console.WriteLine("Ensuring Gss is Initialized ({ApplicationContext})...");
    var output = EnsureGssInitialized();
    Console.WriteLine($"GSS init output: {output}");
}

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable(Constants.Config.Environment.CurrentEnvironment) ?? Constants.Config.Environment.Production;
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environment}.json")
    .AddEnvironmentVariables();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.ListenAnyIP(443, listenOptions =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            listenOptions.UseHttps();
        }
        else
        {
            var certPassword = Environment.GetEnvironmentVariable(Constants.Config.Certificate.CertPassword);
            listenOptions.UseHttps(Constants.Config.Certificate.CertFilePath, certPassword);
        }
    });
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/opt/app-root/.aspnet/DataProtection-Keys"));

builder.Services.AddControllers();
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
    var pendingMigrations = context.Database.GetPendingMigrations().ToArray();
    if (pendingMigrations.Any())
    {
        throw new InvalidOperationException(
            "The database is not up to date with the latest schema. " +
            "Pending migrations: " + string.Join(", ", pendingMigrations));
    }
}

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
