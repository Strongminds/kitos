using System.Diagnostics;
using System.Text.RegularExpressions;
using Npgsql;
using Tools.MigrateSQLServer2Postgres.Cli;

namespace Tools.MigrateSQLServer2Postgres.Migration;

internal sealed class PostgresTargetBootstrapper
{
    private const string PostgresProviderName = "PostgreSql";
    private const string EfMigrationsHistoryTable = "__EFMigrationsHistory";
    private const string EfMigrationsHistorySchema = "dbo";
    private static readonly Regex CreateIndexRegex = new(
        "^(CREATE\\s+(?:UNIQUE\\s+)?INDEX\\s+\")([^\"]+)(\"\\s+ON\\s+\")([^\"]+)(\"\\s*\\()",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task BootstrapFreshDatabaseAsync(string targetConnectionString)
    {
        var repositoryLayout = RepositoryLayoutLocator.Find();

        CliConsole.Info($"Applying canonical PostgreSQL baseline from {repositoryLayout.PostgresBaselineScriptPath}.");
        await ApplyBaselineSchemaAsync(targetConnectionString, repositoryLayout.PostgresBaselineScriptPath);

        CliConsole.Info("Initializing EF Core migration history for the baseline schema.");
        await InitializeBaselineHistoryAsync(targetConnectionString, repositoryLayout);

        CliConsole.Info("Applying PostgreSQL migrations added after the baseline schema.");
        await ApplyPendingEfCoreMigrationsAsync(targetConnectionString, repositoryLayout);
    }

    private static async Task ApplyBaselineSchemaAsync(string targetConnectionString, string baselineScriptPath)
    {
        var baselineSql = await File.ReadAllTextAsync(baselineScriptPath);
        var normalizedSql = NormalizePostgresBaselineScript(baselineSql);

        await using var targetConnection = new NpgsqlConnection(targetConnectionString);
        await targetConnection.OpenAsync();

        await using var command = new NpgsqlCommand(normalizedSql, targetConnection)
        {
            CommandTimeout = 0
        };

        await command.ExecuteNonQueryAsync();
    }

    private static async Task InitializeBaselineHistoryAsync(string targetConnectionString, RepositoryLayout repositoryLayout)
    {
        var productVersion = GetEfCoreProductVersion(repositoryLayout.DbMigrationsScriptPath);
        var coverageCutoff = GetBaselineCoverageCutoff(repositoryLayout.PostgresBaselineScriptPath);
        var coveredMigrationIds = GetMigrationIdsCoveredByBaseline(repositoryLayout.EfCoreMigrationsDirectoryPath, coverageCutoff);

        var tableSetup = $@"
CREATE SCHEMA IF NOT EXISTS {SchemaDiscovery.QuotePostgresIdentifier(EfMigrationsHistorySchema)};
CREATE TABLE IF NOT EXISTS {SchemaDiscovery.QuotePostgresIdentifier(EfMigrationsHistorySchema)}.{SchemaDiscovery.QuotePostgresIdentifier(EfMigrationsHistoryTable)}
(
    ""MigrationId"" character varying(150) NOT NULL,
    ""ProductVersion"" character varying(32) NOT NULL,
    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
);";

        await using var targetConnection = new NpgsqlConnection(targetConnectionString);
        await targetConnection.OpenAsync();

        await using (var setupCommand = new NpgsqlCommand(tableSetup, targetConnection))
        {
            await setupCommand.ExecuteNonQueryAsync();
        }

        foreach (var migrationId in coveredMigrationIds)
        {
            var insertSql = $@"INSERT INTO {SchemaDiscovery.QuotePostgresIdentifier(EfMigrationsHistorySchema)}.{SchemaDiscovery.QuotePostgresIdentifier(EfMigrationsHistoryTable)}
(""MigrationId"", ""ProductVersion"")
VALUES (@migrationId, @productVersion)
ON CONFLICT DO NOTHING;";
            await using var insertCommand = new NpgsqlCommand(insertSql, targetConnection);
            insertCommand.Parameters.AddWithValue("@migrationId", migrationId);
            insertCommand.Parameters.AddWithValue("@productVersion", productVersion);
            await insertCommand.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Reads the optional <c>-- baseline-covers-to: &lt;timestamp&gt;</c> marker from the
    /// first few lines of the baseline SQL file. Returns the timestamp string (e.g. "20260420093000")
    /// if present, otherwise null (only the InitialBaseline migration will be pre-marked).
    /// </summary>
    private static string? GetBaselineCoverageCutoff(string baselineScriptPath)
    {
        foreach (var line in File.ReadLines(baselineScriptPath).Take(10))
        {
            var match = Regex.Match(line, @"--\s*baseline-covers-to:\s*(\d+)", RegexOptions.CultureInvariant);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns all migration IDs (filenames without extension) whose leading timestamp is
    /// less than or equal to <paramref name="coverageCutoff"/>.
    /// If <paramref name="coverageCutoff"/> is null, only the InitialBaseline migration is returned.
    /// </summary>
    private static IReadOnlyList<string> GetMigrationIdsCoveredByBaseline(
        string efCoreMigrationsDirectoryPath,
        string? coverageCutoff)
    {
        var allMigrationIds = Directory
            .GetFiles(efCoreMigrationsDirectoryPath, "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !name.EndsWith(".Designer", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (coverageCutoff is null)
        {
            var baseline = allMigrationIds.FirstOrDefault(id =>
                id.EndsWith("_InitialBaseline", StringComparison.Ordinal));
            return baseline is null ? [] : [baseline];
        }

        return allMigrationIds
            .Where(id =>
            {
                // Migration filenames begin with a 14-digit timestamp prefix (yyyyMMddHHmmss)
                var timestamp = id.Length >= 14 ? id[..14] : id;
                return string.Compare(timestamp, coverageCutoff, StringComparison.Ordinal) <= 0;
            })
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    private static async Task ApplyPendingEfCoreMigrationsAsync(string targetConnectionString, RepositoryLayout repositoryLayout)
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings__KitosContext"] = targetConnectionString,
            ["Database__Provider"] = PostgresProviderName,
            ["IgnorePendingModelChangesWarning"] = "true"
        };

        var bundleStatus = GetPostgresBundleStatus(repositoryLayout);
        if (bundleStatus.CanUseBundle)
        {
            CliConsole.Info($"Using up-to-date PostgreSQL migrations bundle {bundleStatus.BundlePath}.");
            await RunProcessAsync(
                fileName: bundleStatus.BundlePath!,
                arguments: ["--connection", targetConnectionString],
                workingDirectory: repositoryLayout.RootPath,
                environment: environment,
                failureMessage: "The PostgreSQL migrations bundle failed while applying post-baseline migrations.");
            return;
        }

        if (bundleStatus.BundlePath is not null)
        {
            CliConsole.Warning(
                $"Skipping stale PostgreSQL migrations bundle {bundleStatus.BundlePath}. " +
                $"Latest migration input is newer: {bundleStatus.NewestInputPath} ({bundleStatus.NewestInputTimestampUtc:O}) > bundle ({bundleStatus.BundleTimestampUtc:O}).");
        }
        else
        {
            CliConsole.Warning("No PostgreSQL migrations bundle was found. Falling back to dotnet ef against the current repository.");
        }

        CliConsole.Info("Running dotnet ef against the current repository so the latest migrations are applied.");
        await RunProcessAsync(
            fileName: "dotnet",
            arguments:
            [
                "ef",
                "database",
                "update",
                "--project",
                repositoryLayout.InfrastructureProjectPath,
                "--startup-project",
                repositoryLayout.StartupProjectPath,
                "--connection",
                targetConnectionString
            ],
            workingDirectory: repositoryLayout.RootPath,
            environment: environment,
            failureMessage: BuildDotnetEfFailureMessage(bundleStatus));
    }

    private static async Task RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        string failureMessage)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var variable in environment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var standardOutput = await stdOutTask;
        var standardError = await stdErrTask;

        if (process.ExitCode == 0)
        {
            return;
        }

        var output = string.Join(
            Environment.NewLine,
            new[] { standardOutput, standardError }
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => text.Trim()));

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(output)
                ? failureMessage
                : $"{failureMessage}{Environment.NewLine}{output}");
    }

    private static string NormalizePostgresBaselineScript(string sql)
    {
        var normalizedLineEndings = sql.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalizedLineEndings.Split('\n');
        var seenIndexNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var usedFinalIndexNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var match = CreateIndexRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var indexName = match.Groups[2].Value;
            var tableName = match.Groups[4].Value;

            if (!seenIndexNames.TryGetValue(indexName, out var occurrences))
            {
                seenIndexNames[indexName] = 1;
                var finalName = GetUniquePostgresIdentifier(indexName, usedFinalIndexNames);
                usedFinalIndexNames.Add(finalName);
                if (!string.Equals(finalName, indexName, StringComparison.Ordinal))
                {
                    lines[index] = ReplaceIndexName(line, match, finalName);
                }

                continue;
            }

            occurrences++;
            seenIndexNames[indexName] = occurrences;

            var suffix = $"_{tableName}_{occurrences}";
            var newIndexName = GetUniquePostgresIdentifier(indexName + suffix, usedFinalIndexNames);
            usedFinalIndexNames.Add(newIndexName);
            lines[index] = ReplaceIndexName(line, match, newIndexName);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string ReplaceIndexName(string line, Match match, string newIndexName)
    {
        return string.Concat(
            line.AsSpan(0, match.Groups[2].Index),
            newIndexName,
            line.AsSpan(match.Groups[2].Index + match.Groups[2].Length));
    }

    private static string GetUniquePostgresIdentifier(string candidateName, ISet<string> usedNames)
    {
        const int maxLength = 63;
        var baseName = candidateName.Length > maxLength
            ? candidateName[..maxLength]
            : candidateName;

        if (!usedNames.Contains(baseName))
        {
            return baseName;
        }

        for (var counter = 2; ; counter++)
        {
            var suffix = $"_{counter}";
            var prefixLength = maxLength - suffix.Length;
            var prefix = candidateName.Length > prefixLength
                ? candidateName[..prefixLength]
                : candidateName;
            var variant = prefix + suffix;
            if (!usedNames.Contains(variant))
            {
                return variant;
            }
        }
    }

    private static string GetEfCoreProductVersion(string dbMigrationsScriptPath)
    {
        var scriptContents = File.ReadAllText(dbMigrationsScriptPath);
        var match = Regex.Match(
            scriptContents,
            @"VALUES \('\$migrationId', '([^']+)'\)",
            RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            throw new InvalidOperationException("Could not determine the EF Core product version from DbMigrations.ps1.");
        }

        return match.Groups[1].Value;
    }

    private static BundleStatus GetPostgresBundleStatus(RepositoryLayout repositoryLayout)
    {
        var newestInput = GetNewestMigrationInput(repositoryLayout);
        var existingBundle = repositoryLayout.PostgresMigrationsBundleCandidatePaths
            .Where(File.Exists)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (existingBundle is null)
        {
            return new BundleStatus(
                BundlePath: null,
                BundleTimestampUtc: null,
                NewestInputPath: newestInput.Path,
                NewestInputTimestampUtc: newestInput.LastWriteTimeUtc,
                CanUseBundle: false);
        }

        var canUseBundle = existingBundle.LastWriteTimeUtc >= newestInput.LastWriteTimeUtc;
        return new BundleStatus(
            BundlePath: existingBundle.FullName,
            BundleTimestampUtc: existingBundle.LastWriteTimeUtc,
            NewestInputPath: newestInput.Path,
            NewestInputTimestampUtc: newestInput.LastWriteTimeUtc,
            CanUseBundle: canUseBundle);
    }

    private static MigrationInputFile GetNewestMigrationInput(RepositoryLayout repositoryLayout)
    {
        var candidateFiles = new[]
            {
                repositoryLayout.KitosContextPath,
                repositoryLayout.DesignTimeFactoryPath
            }
            .Concat(Directory.GetFiles(repositoryLayout.EfCoreMigrationsDirectoryPath, "*.cs"))
            .Select(path => new FileInfo(path))
            .Where(file => file.Exists)
            .ToList();

        var newest = candidateFiles
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (newest is null)
        {
            throw new InvalidOperationException("Could not determine the latest migration input file for PostgreSQL bundle freshness checks.");
        }

        return new MigrationInputFile(newest.FullName, newest.LastWriteTimeUtc);
    }

    private static string BuildDotnetEfFailureMessage(BundleStatus bundleStatus)
    {
        if (bundleStatus.BundlePath is null)
        {
            return "dotnet ef failed while applying PostgreSQL migrations after the baseline schema. No PostgreSQL bundle was available as a fallback.";
        }

        if (!bundleStatus.CanUseBundle)
        {
            return
                "dotnet ef failed while applying PostgreSQL migrations after the baseline schema. " +
                $"The available PostgreSQL bundle is stale ({bundleStatus.BundlePath}); rebuild it with DeploymentScripts/PrepareDbMigrationsBundle.ps1 or continue using dotnet ef from the current repo.";
        }

        return "dotnet ef failed while applying PostgreSQL migrations after the baseline schema.";
    }
}

internal sealed record RepositoryLayout(
    string RootPath,
    string PostgresBaselineScriptPath,
    string InfrastructureProjectPath,
    string StartupProjectPath,
    IReadOnlyList<string> PostgresMigrationsBundleCandidatePaths,
    string EfCoreMigrationsDirectoryPath,
    string DbMigrationsScriptPath,
    string KitosContextPath,
    string DesignTimeFactoryPath);

internal sealed record BundleStatus(
    string? BundlePath,
    DateTime? BundleTimestampUtc,
    string NewestInputPath,
    DateTime NewestInputTimestampUtc,
    bool CanUseBundle);

internal sealed record MigrationInputFile(
    string Path,
    DateTime LastWriteTimeUtc);

internal static class RepositoryLayoutLocator
{
    public static RepositoryLayout Find()
    {
        foreach (var startPath in GetStartPaths())
        {
            var repositoryLayout = TryFindFrom(startPath);
            if (repositoryLayout is not null)
            {
                return repositoryLayout;
            }
        }

        throw new InvalidOperationException("Could not locate the repository root containing DeploymentScripts and the EF Core projects.");
    }

    private static IEnumerable<string> GetStartPaths()
    {
        yield return AppContext.BaseDirectory;
        yield return Directory.GetCurrentDirectory();
        yield return typeof(RepositoryLayoutLocator).Assembly.Location;
    }

    private static RepositoryLayout? TryFindFrom(string startPath)
    {
        var directoryPath = File.Exists(startPath)
            ? Path.GetDirectoryName(startPath)
            : startPath;

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return null;
        }

        for (var current = new DirectoryInfo(directoryPath); current is not null; current = current.Parent)
        {
            var baselineScriptPath = Path.Combine(current.FullName, "DeploymentScripts", "Baseline.PostgreSql.FullModel.sql");
            var infrastructureProjectPath = Path.Combine(current.FullName, "Infrastructure.DataAccess", "Infrastructure.DataAccess.csproj");
            var startupProjectPath = Path.Combine(current.FullName, "Presentation.Web", "Presentation.Web.csproj");
            var efCoreMigrationsDirectoryPath = Path.Combine(current.FullName, "Infrastructure.DataAccess", "Migrations", "EfCore");
            var dbMigrationsScriptPath = Path.Combine(current.FullName, "DeploymentScripts", "DbMigrations.ps1");
            var kitosContextPath = Path.Combine(current.FullName, "Infrastructure.DataAccess", "KitosContext.cs");
            var designTimeFactoryPath = Path.Combine(current.FullName, "Infrastructure.DataAccess", "KitosContextDesignTimeFactory.cs");
            if (!File.Exists(baselineScriptPath)
                || !File.Exists(infrastructureProjectPath)
                || !File.Exists(startupProjectPath)
                || !Directory.Exists(efCoreMigrationsDirectoryPath)
                || !File.Exists(dbMigrationsScriptPath)
                || !File.Exists(kitosContextPath)
                || !File.Exists(designTimeFactoryPath))
            {
                continue;
            }

            return new RepositoryLayout(
                current.FullName,
                baselineScriptPath,
                infrastructureProjectPath,
                startupProjectPath,
                [
                    Path.Combine(current.FullName, "MigrationsBundle", "efbundle.postgresql.exe"),
                    Path.Combine(current.FullName, "Output", "MigrationsBundle", "efbundle.postgresql.exe")
                ],
                efCoreMigrationsDirectoryPath,
                dbMigrationsScriptPath,
                kitosContextPath,
                designTimeFactoryPath);
        }

        return null;
    }
}
