using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Tests.Infrastructure;

public sealed class PostgreSqlMigrationTest : IAsyncLifetime
{
    private const string DatabaseName = "kitos";
    private const string UserName = "postgres";
    private const string Password = "postgres";
    private const string ConnectionStringEnvVar = "ConnectionStrings__KitosContext";
    private const string ProviderEnvVar = "Database__Provider";
    private const string IgnorePendingModelChangesWarningEnvVar = "IgnorePendingModelChangesWarning";
    private const string EfCoreProductVersion = "10.0.6";

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
    public async Task Can_run_all_migrations_against_postgresql_container()
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        var previousProvider = Environment.GetEnvironmentVariable(ProviderEnvVar);
        var previousIgnorePendingModelChangesWarning = Environment.GetEnvironmentVariable(IgnorePendingModelChangesWarningEnvVar);

        try
        {
            var connectionString =
                $"Host={_container.Hostname};Port={_container.GetMappedPublicPort(5432)};Database={DatabaseName};Username={UserName};Password={Password}";
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, connectionString);
            Environment.SetEnvironmentVariable(ProviderEnvVar, "postgresql");
            Environment.SetEnvironmentVariable(IgnorePendingModelChangesWarningEnvVar, "true");

            var factory = new KitosContextDesignTimeFactory();
            await using var context = factory.CreateDbContext([]);

            await BootstrapPostgreSqlBaselineAsync(context);
            await context.Database.MigrateAsync();

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

    private static async Task BootstrapPostgreSqlBaselineAsync(KitosContext context)
    {
        var baselineScriptPath = FindBaselineScriptPath();
        var baselineSql = NormalizeDuplicatePostgreSqlIndexNames(await File.ReadAllTextAsync(baselineScriptPath));
        await context.Database.ExecuteSqlRawAsync(baselineSql);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS dbo."__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );
            """
        );

        await MarkMigrationAsAppliedAsync(context, "20260413095837_InitialBaseline");
        await MarkMigrationAsAppliedAsync(context, "20260415045340_AddExternalAndInternalPaymentOrganizationUnits_ToContractReadModel");
        await MarkMigrationAsAppliedAsync(context, "20260420093000_BridgeMissingColumnsFromEF6");
        await MarkMigrationAsAppliedAsync(context, "20260427113000_EnableCitextForCaseInsensitiveNameColumns");
    }

    private static Task MarkMigrationAsAppliedAsync(KitosContext context, string migrationId)
    {
        return context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO dbo."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
             VALUES ({migrationId}, {EfCoreProductVersion})
             ON CONFLICT DO NOTHING;
             """
        );
    }

    private static string FindBaselineScriptPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "DeploymentScripts", "Baseline.PostgreSql.FullModel.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate DeploymentScripts\\Baseline.PostgreSql.FullModel.sql");
    }

    private static string NormalizeDuplicatePostgreSqlIndexNames(string sql)
    {
        var countsByIndexName = new Dictionary<string, int>(StringComparer.Ordinal);
        var usedFinalNames = new HashSet<string>(StringComparer.Ordinal);
        var pattern = new Regex(
            """^(?<prefix>\s*CREATE(?:\s+UNIQUE)?\s+INDEX\s+")(?<index>[^"]+)(?<suffix>" ON "(?<table>[^"]+)".*)$""",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

        return pattern.Replace(sql, match =>
        {
            var indexName = match.Groups["index"].Value;
            var tableName = match.Groups["table"].Value;
            var candidateName = indexName;

            if (!countsByIndexName.TryGetValue(indexName, out var count))
            {
                countsByIndexName[indexName] = 1;
            }
            else
            {
                count++;
                countsByIndexName[indexName] = count;
                candidateName = $"{indexName}_{tableName}_{count}";
            }

            var finalName = GetUniquePostgreSqlIdentifier(candidateName, usedFinalNames);
            if (finalName == indexName)
            {
                return match.Value;
            }

            return $"{match.Groups["prefix"].Value}{finalName}{match.Groups["suffix"].Value}";
        });
    }

    private static string GetUniquePostgreSqlIdentifier(string candidate, ISet<string> usedNames)
    {
        const int maxLength = 63;
        var normalized = Truncate(candidate, maxLength);

        if (usedNames.Add(normalized))
        {
            return normalized;
        }

        for (var suffixNumber = 2; ; suffixNumber++)
        {
            var suffix = $"_{suffixNumber}";
            var withSuffix = $"{Truncate(candidate, maxLength - suffix.Length)}{suffix}";
            if (usedNames.Add(withSuffix))
            {
                return withSuffix;
            }
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
