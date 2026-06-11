using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using Group = System.Text.RegularExpressions.Group;

namespace Presentation.Web
{
    // Bootstraps a fresh PostgreSQL database by applying the full baseline schema and pre-marking
    // migrations whose schema is already captured in the baseline SQL. This mirrors the logic in
    // DbMigrations.ps1 Initialize-EFCoreHistoryForNewPostgresDb and must stay in sync with it.
    internal static class PostgresFreshDatabaseBootstrapper
    {
        private const string EfCoreProductVersion = "10.0.6";
        private const int MaxPostgresIdentifierLength = 63;
        private const string BaselineSqlFileName = "Baseline.PostgreSql.FullModel.sql";

        private static readonly string[] BaselineMigrationIds =
        {
            "20260413095837_InitialBaseline",
            "20260415045340_AddExternalAndInternalPaymentOrganizationUnits_ToContractReadModel",
            "20260420093000_BridgeMissingColumnsFromEF6",
            "20260427113000_EnableCitextForCaseInsensitiveNameColumns"
        };

        public static void BootstrapIfFresh(KitosContext db)
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            if (MigrationsHistoryExists(connection))
            {
                Log.Information("Existing PostgreSQL database detected; skipping baseline bootstrap.");
                return;
            }

            Log.Information("Fresh PostgreSQL database detected; applying baseline schema...");
            ApplyBaselineSchema(connection);
            PreMarkBaselineMigrations(connection);
            Log.Information("Baseline schema applied and baseline migrations pre-marked.");
        }

        private static bool MigrationsHistoryExists(DbConnection connection)
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText =
                "SELECT 1 FROM information_schema.tables " +
                "WHERE table_schema = 'dbo' AND table_name = '__EFMigrationsHistory'";

            return checkCmd.ExecuteScalar() != null;
        }

        private static void ApplyBaselineSchema(DbConnection connection)
        {
            var baselinePath = Path.Combine(AppContext.BaseDirectory, BaselineSqlFileName);
            if (!File.Exists(baselinePath))
                throw new FileNotFoundException($"PostgreSQL baseline SQL not found. Expected path: {baselinePath}");

            using var baselineCmd = connection.CreateCommand();
            baselineCmd.CommandText = NormalizeBaselineSql(File.ReadAllText(baselinePath));
            baselineCmd.ExecuteNonQuery();
        }

        private static void PreMarkBaselineMigrations(DbConnection connection)
        {
            EnsureMigrationsHistoryTable(connection);

            foreach (var migrationId in BaselineMigrationIds)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = """
                    INSERT INTO dbo."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES (@migrationId, @productVersion) ON CONFLICT DO NOTHING;
                    """;

                var migrationIdParameter = insertCmd.CreateParameter();
                migrationIdParameter.ParameterName = "migrationId";
                migrationIdParameter.Value = migrationId;
                insertCmd.Parameters.Add(migrationIdParameter);

                var productVersionParameter = insertCmd.CreateParameter();
                productVersionParameter.ParameterName = "productVersion";
                productVersionParameter.Value = EfCoreProductVersion;
                insertCmd.Parameters.Add(productVersionParameter);

                insertCmd.ExecuteNonQuery();
            }
        }

        private static void EnsureMigrationsHistoryTable(DbConnection connection)
        {
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = """
                CREATE SCHEMA IF NOT EXISTS dbo;
                CREATE TABLE IF NOT EXISTS dbo."__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """;

            createTableCmd.ExecuteNonQuery();
        }

        // Replicates the duplicate-index-name normalization from DbMigrations.ps1 Get-NormalizedPostgresSqlFile.
        // PostgreSQL requires index names to be unique within a schema, but the generated baseline SQL reuses
        // names like "UX_Option_Uuid" across many option tables. Duplicates are renamed by appending
        // "_<tableName>_<N>" and truncating to the 63-character PostgreSQL identifier limit.
        private static string NormalizeBaselineSql(string sql)
        {
            var lines = sql.Split('\n');
            var seenIndexNames = new Dictionary<string, int>(StringComparer.Ordinal);
            var usedFinalNames = new HashSet<string>(StringComparer.Ordinal);
            var indexPattern = new Regex(
                @"^(CREATE\s+(?:UNIQUE\s+)?INDEX\s+"")([^""]+)(""\s+ON\s+"")([^""]+)(""\s*\()",
                RegexOptions.IgnoreCase);

            for (var i = 0; i < lines.Length; i++)
            {
                var match = indexPattern.Match(lines[i]);
                if (!match.Success)
                    continue;

                var indexName = match.Groups[2].Value;
                var tableName = match.Groups[4].Value;

                if (!seenIndexNames.ContainsKey(indexName))
                {
                    seenIndexNames[indexName] = 1;
                    var finalName = GetUniquePostgresIdentifier(indexName, usedFinalNames);
                    usedFinalNames.Add(finalName);
                    if (finalName != indexName)
                        lines[i] = ReplaceGroup(lines[i], match.Groups[2], finalName);

                    continue;
                }

                seenIndexNames[indexName]++;
                var suffix = $"_{tableName}_{seenIndexNames[indexName]}";
                var newName = GetUniquePostgresIdentifier(indexName + suffix, usedFinalNames);
                usedFinalNames.Add(newName);
                lines[i] = ReplaceGroup(lines[i], match.Groups[2], newName);
            }

            return string.Join('\n', lines);
        }

        private static string GetUniquePostgresIdentifier(string candidate, HashSet<string> used)
        {
            var truncated = candidate.Length > MaxPostgresIdentifierLength
                ? candidate[..MaxPostgresIdentifierLength]
                : candidate;

            if (!used.Contains(truncated))
                return truncated;

            for (var counter = 2; ; counter++)
            {
                var suffix = $"_{counter}";
                var prefixLen = MaxPostgresIdentifierLength - suffix.Length;
                var prefix = candidate.Length > prefixLen ? candidate[..prefixLen] : candidate;
                var variant = prefix + suffix;
                if (!used.Contains(variant))
                    return variant;
            }
        }

        private static string ReplaceGroup(string line, Group group, string replacement) =>
            line[..group.Index] + replacement + line[(group.Index + group.Length)..];
    }
}
