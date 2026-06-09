using AutoMapper;
using Infrastructure.DataAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Presentation.Web;
using Presentation.Web.Infrastructure.Configuration;
using Presentation.Web.Infrastructure.DI;
using Presentation.Web.Models.Application.RuntimeEnv;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

// Must be set before any Npgsql type is loaded (including Hangfire's PostgreSQL storage).
// Allows writing DateTime with Kind=UTC to 'timestamp without time zone' columns.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Digst.OioIdws.* assemblies are IL-patched local DLLs (not NuGet packages) referenced
// as "type:reference" in deps.json. The runtime's assembly loader skips those entries
// and the AppContext.BaseDirectory fallback can fail under some hosting models (IIS,
// dotnet watch). Registering an explicit resolver here ensures they are always found.
System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += static (context, name) =>
{
    if (name.Name?.StartsWith("Digst.OioIdws.", StringComparison.Ordinal) != true)
        return null;
    var path = System.IO.Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");
    return System.IO.File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var configuration = builder.Configuration;
var services = builder.Services;

services.AddKitosMvc();
services.AddRouting();
var signingKey = services.AddKitosAuthentication(configuration);
var kitosEnv = KitosEnvironmentConfiguration.FromConfiguration(configuration).Environment;
services.AddKitosSwagger(kitosEnv is not (KitosEnvironment.Production or KitosEnvironment.Staging));
services.AddKitosHangfire(configuration);

// AutoMapper - using explicit assembly scanning
var mapperConfig = new MapperConfiguration(cfg => cfg.AddMaps(typeof(MappingConfig).Assembly), NullLoggerFactory.Instance);
services.AddSingleton(mapperConfig.CreateMapper());

// HttpContext accessor
services.AddHttpContextAccessor();

// KITOS services (DI registrations)
KitosServiceRegistration.Register(services, configuration, signingKey);

var app = builder.Build();

// Support --migrate-and-exit for running EF migrations in init-containers or compose services.
// For a fresh PostgreSQL database the baseline schema and migration history must be bootstrapped
// before EF Core migrations run, because InitialBaseline.Up() is intentionally empty.
if (args.Contains("--migrate-and-exit", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KitosContext>();
    var isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    if (isPostgres)
        BootstrapPostgresIfFresh(db);

    Log.Information("Applying pending EF Core migrations...");
    db.Database.Migrate();
    Log.Information("Migrations applied successfully.");
    return;
}

// Initialize the SAML library's static HTTP context accessor so it can access HttpContext.Current
// during SAML flows without requiring DI injection into the (statically-instantiated) handler classes.
dk.nita.saml20.Utils.SamlHttpContextAccessor.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>());

app.UseKitosPipeline();
app.MapKitosEndpoints();
app.InitializeHangfireJobs();

app.Run();

// Bootstraps a fresh PostgreSQL database by applying the full baseline schema and pre-marking
// migrations whose schema is already captured in the baseline SQL. This mirrors the logic in
// DbMigrations.ps1 Initialize-EFCoreHistoryForNewPostgresDb and must stay in sync with it.
static void BootstrapPostgresIfFresh(KitosContext db)
{
    var connection = db.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
        connection.Open();

    using var checkCmd = connection.CreateCommand();
    checkCmd.CommandText =
        "SELECT 1 FROM information_schema.tables " +
        "WHERE table_schema = 'dbo' AND table_name = '__EFMigrationsHistory'";
    var historyExists = checkCmd.ExecuteScalar() != null;

    if (historyExists)
    {
        Log.Information("Existing PostgreSQL database detected; skipping baseline bootstrap.");
        return;
    }

    Log.Information("Fresh PostgreSQL database detected; applying baseline schema...");

    var baselinePath = Path.Combine(AppContext.BaseDirectory, "Baseline.PostgreSql.FullModel.sql");
    if (!File.Exists(baselinePath))
        throw new FileNotFoundException(
            $"PostgreSQL baseline SQL not found. Expected path: {baselinePath}");

    using var baselineCmd = connection.CreateCommand();
    baselineCmd.CommandText = NormalizeBaselineSql(File.ReadAllText(baselinePath));
    baselineCmd.ExecuteNonQuery();

    // Pre-mark migrations already included in the baseline so EF Core does not re-apply them.
    // Any migration added here must have its full schema captured in Baseline.PostgreSql.FullModel.sql.
    // Keep this list in sync with DbMigrations.ps1 Initialize-EFCoreHistoryForNewPostgresDb.
    const string preMarkSql = """
        CREATE SCHEMA IF NOT EXISTS dbo;
        CREATE TABLE IF NOT EXISTS dbo."__EFMigrationsHistory" (
            "MigrationId" character varying(150) NOT NULL,
            "ProductVersion" character varying(32) NOT NULL,
            CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
        );
        INSERT INTO dbo."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260413095837_InitialBaseline', '10.0.6') ON CONFLICT DO NOTHING;
        INSERT INTO dbo."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260415045340_AddExternalAndInternalPaymentOrganizationUnits_ToContractReadModel', '10.0.6') ON CONFLICT DO NOTHING;
        INSERT INTO dbo."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260420093000_BridgeMissingColumnsFromEF6', '10.0.6') ON CONFLICT DO NOTHING;
        INSERT INTO dbo."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260427113000_EnableCitextForCaseInsensitiveNameColumns', '10.0.6') ON CONFLICT DO NOTHING;
        """;

    using var preMarkCmd = connection.CreateCommand();
    preMarkCmd.CommandText = preMarkSql;
    preMarkCmd.ExecuteNonQuery();

    Log.Information("Baseline schema applied and baseline migrations pre-marked.");
}

// Replicates the duplicate-index-name normalization from DbMigrations.ps1 Get-NormalizedPostgresSqlFile.
// PostgreSQL requires index names to be unique within a schema, but the generated baseline SQL reuses
// names like "UX_Option_Uuid" across many option tables. Duplicates are renamed by appending
// "_<tableName>_<N>" and truncating to the 63-character PostgreSQL identifier limit.
static string NormalizeBaselineSql(string sql)
{
    const int MaxIdentifierLength = 63;
    var lines = sql.Split('\n');
    var seenIndexNames = new Dictionary<string, int>(StringComparer.Ordinal);
    var usedFinalNames = new HashSet<string>(StringComparer.Ordinal);
    var indexPattern = new Regex(
        @"^(CREATE\s+(?:UNIQUE\s+)?INDEX\s+"")([^""]+)(""\s+ON\s+"")([^""]+)(""\s*\()",
        RegexOptions.IgnoreCase);

    for (var i = 0; i < lines.Length; i++)
    {
        var match = indexPattern.Match(lines[i]);
        if (!match.Success) continue;

        var indexName = match.Groups[2].Value;
        var tableName = match.Groups[4].Value;

        if (!seenIndexNames.ContainsKey(indexName))
        {
            seenIndexNames[indexName] = 1;
            var finalName = GetUniquePostgresIdentifier(indexName, usedFinalNames, MaxIdentifierLength);
            usedFinalNames.Add(finalName);
            if (finalName != indexName)
                lines[i] = ReplaceGroup(lines[i], match.Groups[2], finalName);
        }
        else
        {
            seenIndexNames[indexName]++;
            var suffix = $"_{tableName}_{seenIndexNames[indexName]}";
            var newName = GetUniquePostgresIdentifier(indexName + suffix, usedFinalNames, MaxIdentifierLength);
            usedFinalNames.Add(newName);
            lines[i] = ReplaceGroup(lines[i], match.Groups[2], newName);
        }
    }

    return string.Join('\n', lines);
}

static string GetUniquePostgresIdentifier(string candidate, HashSet<string> used, int maxLength)
{
    var truncated = candidate.Length > maxLength ? candidate[..maxLength] : candidate;
    if (!used.Contains(truncated)) return truncated;

    for (var counter = 2; ; counter++)
    {
        var suffix = $"_{counter}";
        var prefixLen = maxLength - suffix.Length;
        var prefix = candidate.Length > prefixLen ? candidate[..prefixLen] : candidate;
        var variant = prefix + suffix;
        if (!used.Contains(variant)) return variant;
    }
}

static string ReplaceGroup(string line, Group group, string replacement) =>
    line[..group.Index] + replacement + line[(group.Index + group.Length)..];
