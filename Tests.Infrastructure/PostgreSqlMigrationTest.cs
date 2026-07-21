using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Tests.Infrastructure;

public sealed class PostgreSqlMigrationTest : IAsyncLifetime
{
    private const string DatabaseName = "kitos";
    private const string UserName = "postgres";
    private const string Password = "postgres";
    private const string ConnectionStringEnvVar = "ConnectionStrings__KitosContext";
    private const string ProviderEnvVar = "Database__Provider";
    private const string IgnorePendingModelChangesWarningEnvVar = "IgnorePendingModelChangesWarning";

    private readonly IContainer _container = new ContainerBuilder("postgres:16-alpine")
        .WithEnvironment("POSTGRES_DB", DatabaseName)
        .WithEnvironment("POSTGRES_USER", UserName)
        .WithEnvironment("POSTGRES_PASSWORD", Password)
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
        .Build();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Can_Run_Migrations_Against_Postgresql()
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        var previousProvider = Environment.GetEnvironmentVariable(ProviderEnvVar);
        var previousIgnorePendingModelChangesWarning = Environment.GetEnvironmentVariable(IgnorePendingModelChangesWarningEnvVar);

        try
        {
            var connectionString =
                $"Host={_container.Hostname};Port={_container.GetMappedPublicPort(5432)};Database={DatabaseName};Username={UserName};Password={Password}";
            var hangfireConnectionString =
                $"Host={_container.Hostname};Port={_container.GetMappedPublicPort(5432)};Database=hangfire;Username={UserName};Password={Password}";

            await ExecutePrepareLocalDatabaseScriptAsync(connectionString, hangfireConnectionString);

            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, connectionString);
            Environment.SetEnvironmentVariable(ProviderEnvVar, "postgresql");
            Environment.SetEnvironmentVariable(IgnorePendingModelChangesWarningEnvVar, "true");

            var factory = new KitosContextDesignTimeFactory();
            await using var context = factory.CreateDbContext([]);

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            Assert.Empty(pendingMigrations);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, previousConnectionString);
            Environment.SetEnvironmentVariable(ProviderEnvVar, previousProvider);
            Environment.SetEnvironmentVariable(IgnorePendingModelChangesWarningEnvVar, previousIgnorePendingModelChangesWarning);
        }
    }

    private static async Task ExecutePrepareLocalDatabaseScriptAsync(string kitosConnectionString, string hangfireConnectionString)
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "DeploymentScripts", "PrepareLocalDatabase.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-kitosDbConnectionString");
        startInfo.ArgumentList.Add(kitosConnectionString);
        startInfo.ArgumentList.Add("-hangfireDbConnectionString");
        startInfo.ArgumentList.Add(hangfireConnectionString);

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;
        var output = string.Join(
            Environment.NewLine,
            new[] { stdOut, stdErr }.Where(text => !string.IsNullOrWhiteSpace(text)).Select(text => text.Trim()));

        Assert.True(
            process.ExitCode == 0,
            $"PrepareLocalDatabase.ps1 failed with exit code {process.ExitCode}.{Environment.NewLine}{output}");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "DeploymentScripts", "PrepareLocalDatabase.ps1");
            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing DeploymentScripts\\PrepareLocalDatabase.ps1");
    }
}
