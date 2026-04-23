using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;
using Spectre.Console;
using Tools.MigrateSQLServer2Postgres.Cli;

namespace Tools.MigrateSQLServer2Postgres.Migration;

internal sealed class MigrationRunner
{
    public async Task<int> RunAsync(CommandLineOptions options, bool isInteractive = false)
    {
        await using var sourceConnection = new SqlConnection(options.SourceConnectionString);
        await sourceConnection.OpenAsync();
        CliConsole.Success("SQL Server source connection OK.");

        var effectiveOptions = options;
        var freshTargetPrepared = false;
        NpgsqlConnection? targetConnection = null;

        try
        {
            try
            {
                targetConnection = await OpenTargetConnectionAsync(options.TargetConnectionString);
                CliConsole.Success("PostgreSQL target connection OK.");
            }
            catch (PostgresException exception) when (IsMissingDatabase(exception))
            {
                CliConsole.Warning("Target PostgreSQL database does not exist.");
                var shouldCreate = !isInteractive || AnsiConsole.Confirm("Create a new empty target database with the required structure now?", true);
                if (!shouldCreate)
                {
                    throw new CommandLineException("Target PostgreSQL database does not exist.");
                }

                var prepareResult = await RunPrepareTargetAsync(sourceConnection, options.TargetConnectionString, requireConfirmation: false);
                if (prepareResult != 0)
                {
                    throw new CommandLineException("Failed to create the target PostgreSQL database.");
                }

                effectiveOptions = options with
                {
                    AllowNonEmptyTarget = false,
                    Resume = false
                };
                freshTargetPrepared = true;

                targetConnection = await OpenTargetConnectionAsync(options.TargetConnectionString);
                CliConsole.Success("PostgreSQL target connection OK.");
            }

            if (targetConnection is null)
            {
                throw new CommandLineException("Target PostgreSQL connection could not be established.");
            }

            if (isInteractive)
            {
                // If user explicitly passed --resume or --allow-non-empty-target, skip interactive prompts
                if (!effectiveOptions.Resume && !effectiveOptions.AllowNonEmptyTarget)
                {
                    var adjustedOptions = await RecreateTargetIfUserRequestsItAsync(sourceConnection, targetConnection, effectiveOptions);
                    if (!ReferenceEquals(adjustedOptions, effectiveOptions))
                    {
                        await targetConnection.CloseAsync();
                        await targetConnection.DisposeAsync();
                        targetConnection = await OpenTargetConnectionAsync(options.TargetConnectionString);
                        effectiveOptions = adjustedOptions;
                        freshTargetPrepared = true;
                    }

                    if (!freshTargetPrepared)
                    {
                        var strategyChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Choose [green]migration strategy[/]")
                                .PageSize(10)
                                .AddChoices(
                                    "Start fresh (recreate target database)",
                                    "Continue from previous run (resume)",
                                    "Use current settings"));

                        if (strategyChoice == "Start fresh (recreate target database)")
                        {
                            var prepareResult = await RunPrepareTargetAsync(sourceConnection, effectiveOptions.TargetConnectionString, requireConfirmation: true);
                            if (prepareResult != 0)
                            {
                                return prepareResult;
                            }

                            await targetConnection.CloseAsync();
                            await targetConnection.DisposeAsync();
                            targetConnection = await OpenTargetConnectionAsync(options.TargetConnectionString);

                            effectiveOptions = effectiveOptions with
                            {
                                AllowNonEmptyTarget = false,
                                Resume = false
                            };
                        }
                        else if (strategyChoice == "Continue from previous run (resume)")
                        {
                            effectiveOptions = effectiveOptions with
                            {
                                AllowNonEmptyTarget = true,
                                Resume = true
                            };
                        }
                    }
                }
            }

            var readinessResult = await RunReadinessChecksAsync(sourceConnection, targetConnection, effectiveOptions);
            if (readinessResult != 0)
            {
                return readinessResult;
            }

            var executeResult = await RunExecuteAsync(sourceConnection, targetConnection, effectiveOptions);
            if (executeResult != 0)
            {
                return executeResult;
            }

            return await RunValidateAsync(sourceConnection, targetConnection);
        }
        finally
        {
            if (targetConnection is not null)
            {
                await targetConnection.DisposeAsync();
            }
        }
    }

    private static async Task<int> RunReadinessChecksAsync(SqlConnection sourceConnection, NpgsqlConnection targetConnection, CommandLineOptions options)
    {
        CliConsole.Info("Running readiness checks...");

        var sourceTables = await SchemaDiscovery.GetSqlServerTablesAsync(sourceConnection);
        var targetTables = await SchemaDiscovery.GetPostgresTablesAsync(targetConnection);
        var matchedTables = SchemaDiscovery.MatchTables(sourceTables, targetTables);

        if (matchedTables.Count == 0)
        {
            CliConsole.Error("Readiness checks failed: no common tables were found between source and target.");
            return -1;
        }

        if (!options.AllowNonEmptyTarget)
        {
            var nonEmptyTable = await FindFirstNonEmptyPostgresTableAsync(targetConnection, targetTables);
            CliConsole.RenderReadinessSummary(sourceTables.Count, targetTables.Count, matchedTables.Count, nonEmptyTable is null, nonEmptyTable);
            if (nonEmptyTable is not null)
            {
                CliConsole.Error($"Readiness checks failed: target database is not empty. First non-empty table: {nonEmptyTable}.");
                return -2;
            }

            CliConsole.Success("Readiness checks completed successfully.");
            return 0;
        }

        CliConsole.RenderReadinessSummary(sourceTables.Count, targetTables.Count, matchedTables.Count, true, null);
        CliConsole.Success("Readiness checks completed successfully.");
        return 0;
    }

    private static async Task<int> RunPrepareTargetAsync(SqlConnection sourceConnection, string targetConnectionString, bool requireConfirmation)
    {
        CliConsole.Info("Preparing fresh PostgreSQL target...");

        var targetBuilder = new NpgsqlConnectionStringBuilder(targetConnectionString);
        if (string.IsNullOrWhiteSpace(targetBuilder.Database))
        {
            throw new CommandLineException("Target connection string must include a database name.");
        }

        var targetDatabase = targetBuilder.Database;
        var bootstrapTargetBuilder = new NpgsqlConnectionStringBuilder(targetBuilder.ConnectionString)
        {
            Pooling = false
        };
        var adminBuilder = new NpgsqlConnectionStringBuilder(targetBuilder.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };

        await using (var adminConnection = new NpgsqlConnection(adminBuilder.ConnectionString))
        {
            await adminConnection.OpenAsync();

            var databaseExists = await DatabaseExistsAsync(adminConnection, targetDatabase);
            if (databaseExists)
            {
                CliConsole.Warning($"Existing target database '{targetDatabase}' found.");
                if (requireConfirmation)
                {
                    var overwriteConfirmed = AnsiConsole.Confirm($"Overwrite PostgreSQL database '{targetDatabase}'? This will delete all existing data.", false);
                    if (!overwriteConfirmed)
                    {
                        CliConsole.Warning("Target preparation cancelled.");
                        return -1;
                    }
                }

                await DropDatabaseAsync(adminConnection, targetDatabase);
                NpgsqlConnection.ClearAllPools();
                CliConsole.Info($"Dropped target database '{targetDatabase}'.");
            }

            await CreateDatabaseAsync(adminConnection, targetDatabase);
            CliConsole.Success($"Created target database '{targetDatabase}'.");
        }

        NpgsqlConnection.ClearAllPools();

        await using var targetConnection = new NpgsqlConnection(bootstrapTargetBuilder.ConnectionString);
        await targetConnection.OpenAsync();

        var sourceDefinitions = await SchemaDiscovery.GetSqlServerTableDefinitionsAsync(sourceConnection);
        if (sourceDefinitions.Count == 0)
        {
            CliConsole.Error("No SQL Server tables were found. Target database was created but no schema was generated.");
            return -2;
        }

        await CreateTargetSchemaFromSourceAsync(targetConnection, sourceDefinitions);

        CliConsole.Success("Target schema is ready.");
        return 0;
    }

    private static async Task<NpgsqlConnection> OpenTargetConnectionAsync(string targetConnectionString)
    {
        var targetConnection = new NpgsqlConnection(targetConnectionString);
        await targetConnection.OpenAsync();
        return targetConnection;
    }

    private static async Task<CommandLineOptions> RecreateTargetIfUserRequestsItAsync(
        SqlConnection sourceConnection,
        NpgsqlConnection targetConnection,
        CommandLineOptions options)
    {
        var sourceTables = await SchemaDiscovery.GetSqlServerTablesAsync(sourceConnection);
        var targetTables = await SchemaDiscovery.GetPostgresTablesAsync(targetConnection);
        var matchedTables = SchemaDiscovery.MatchTables(sourceTables, targetTables);

        if (targetTables.Count == 0 || matchedTables.Count == 0)
        {
            CliConsole.Warning("Target database does not contain the required table structure.");
            var createTarget = AnsiConsole.Confirm("Create a new empty target database with the required structure now?", true);
            if (!createTarget)
            {
                return options;
            }

            NpgsqlConnection.ClearPool(targetConnection);
            await targetConnection.CloseAsync();
            await targetConnection.DisposeAsync();
            NpgsqlConnection.ClearAllPools();

            var createStructureResult = await RunPrepareTargetAsync(sourceConnection, options.TargetConnectionString, requireConfirmation: false);
            if (createStructureResult != 0)
            {
                throw new CommandLineException("Failed to create the target PostgreSQL database structure.");
            }

            return options with
            {
                AllowNonEmptyTarget = false,
                Resume = false
            };
        }

        if (options.AllowNonEmptyTarget)
        {
            return options;
        }

        var nonEmptyTable = await FindFirstNonEmptyPostgresTableAsync(targetConnection, targetTables);
        if (nonEmptyTable is null)
        {
            return options;
        }

        CliConsole.Warning($"Target database is not empty. First non-empty table: {nonEmptyTable}.");
        var recreate = AnsiConsole.Confirm("Recreate the target database as a new empty database with the required structure?", true);
        if (!recreate)
        {
            return options;
        }

        NpgsqlConnection.ClearPool(targetConnection);
        await targetConnection.CloseAsync();
        await targetConnection.DisposeAsync();
        NpgsqlConnection.ClearAllPools();

        var prepareResult = await RunPrepareTargetAsync(sourceConnection, options.TargetConnectionString, requireConfirmation: false);
        if (prepareResult != 0)
        {
            throw new CommandLineException("Failed to recreate the target PostgreSQL database.");
        }

        return options with
        {
            AllowNonEmptyTarget = false,
            Resume = false
        };
    }

    private static bool IsMissingDatabase(PostgresException exception)
    {
        return string.Equals(exception.SqlState, PostgresErrorCodes.InvalidCatalogName, StringComparison.Ordinal);
    }

    private static async Task<int> RunExecuteAsync(SqlConnection sourceConnection, NpgsqlConnection targetConnection, CommandLineOptions options)
    {
        CliConsole.Info("Copying data...");

        await MigrationJournal.EnsureJournalTableAsync(targetConnection);

        var sourceTables = await SchemaDiscovery.GetSqlServerTablesAsync(sourceConnection);
        var targetTables = await SchemaDiscovery.GetPostgresTablesAsync(targetConnection);
        var matchedTables = SchemaDiscovery.MatchTables(sourceTables, targetTables);

        if (matchedTables.Count == 0)
        {
            CliConsole.Error("Migration aborted: no common tables were found between source and target.");
            return -1;
        }

        var foreignKeys = await SchemaDiscovery.GetSqlServerForeignKeyEdgesAsync(sourceConnection);
        var sourceTableLookup = matchedTables.ToDictionary(match => match.Source, match => match.Target);
        var orderedTables = SchemaDiscovery.TopologicalSortTables(matchedTables.Select(match => match.Source).ToList(), foreignKeys);

        CliConsole.Info($"Tables scheduled for migration: {orderedTables.Count}");

        if (options.DisableForeignKeyChecks)
        {
            await SetPostgresSessionReplicationRoleAsync(targetConnection, "replica");
            CliConsole.Warning("Foreign key checks are temporarily disabled for this target session.");
        }

        var failures = new List<string>();
        var results = new List<TableExecutionResult>();
        var candidateTables = orderedTables.Where(table => !ShouldSkipTable(table)).ToList();

        try
        {
            await AnsiConsole.Progress()
                .AutoRefresh(true)
                .AutoClear(false)
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn()
                ])
                .StartAsync(async context =>
            {
                var overallTask = context.AddTask("[green]Migrating tables[/]", maxValue: candidateTables.Count == 0 ? 1 : candidateTables.Count);

                foreach (var table in orderedTables)
                {
                    if (ShouldSkipTable(table))
                    {
                        results.Add(new TableExecutionResult(table, TableExecutionStatus.Skipped, null, "Technical/read-model exclusion"));
                        CliConsole.Warning($"Skipping technical/read-model table {table}.");
                        continue;
                    }

                    overallTask.Description = $"[green]Processing {Markup.Escape(table.ToString())}[/]";

                    if (options.Resume && await MigrationJournal.WasSuccessfulAsync(targetConnection, table))
                    {
                        results.Add(new TableExecutionResult(table, TableExecutionStatus.Skipped, null, "Already marked successful in migration journal"));
                        CliConsole.Warning($"Skipping {table} because it is already marked as successful.");
                        overallTask.Increment(1);
                        continue;
                    }

                    try
                    {
                        var targetTable = sourceTableLookup[table];
                        CliConsole.Info($"Mapping: {table} → {targetTable}");
                        await MigrationJournal.MarkStartedAsync(targetConnection, targetTable);
                        var rowsCopied = await CopyTableAsync(sourceConnection, targetConnection, table, targetTable);
                        await ReseedSequenceIfPresentAsync(targetConnection, targetTable);
                        await MigrationJournal.MarkSucceededAsync(targetConnection, targetTable, rowsCopied);
                        results.Add(new TableExecutionResult(table, TableExecutionStatus.Succeeded, rowsCopied));
                    }
                    catch (Exception exception)
                    {
                        var targetTable = sourceTableLookup[table];
                        await MigrationJournal.MarkFailedAsync(targetConnection, targetTable, exception.Message);
                        var message = $"Failed to migrate {table} -> {targetTable}: {exception.Message}";
                        failures.Add(message);
                        results.Add(new TableExecutionResult(table, TableExecutionStatus.Failed, null, exception.Message));
                        CliConsole.Error(message);

                        if (!options.ContinueOnError)
                        {
                            CliConsole.Warning("Stopping on first error. Use --continue-on-error to continue with remaining tables.");
                            overallTask.Increment(1);
                            return;
                        }
                    }

                    overallTask.Increment(1);
                }
            });
        }
        finally
        {
            if (options.DisableForeignKeyChecks)
            {
                await SetPostgresSessionReplicationRoleAsync(targetConnection, "origin");
            }
        }

        CliConsole.RenderExecutionSummary(results);

        if (failures.Count > 0)
        {
            CliConsole.Error($"Migration completed with {failures.Count} failed table(s).");

            if (!options.ContinueOnError)
            {
                return -2;
            }

            return -3;
        }

        CliConsole.Success("Migration completed successfully.");
        return 0;
    }

    private static async Task<int> RunValidateAsync(SqlConnection sourceConnection, NpgsqlConnection targetConnection)
    {
        CliConsole.Info("Validating row counts...");

        var sourceTables = await SchemaDiscovery.GetSqlServerTablesAsync(sourceConnection);
        var targetTables = await SchemaDiscovery.GetPostgresTablesAsync(targetConnection);
        var matchedTables = SchemaDiscovery.MatchTables(sourceTables, targetTables);

        var mismatches = new List<ValidationMismatch>();
        var comparedTableCount = 0;
        foreach (var match in matchedTables.OrderBy(match => match.Source.Schema).ThenBy(match => match.Source.Name))
        {
            if (ShouldSkipTable(match.Source))
            {
                continue;
            }

            comparedTableCount++;
            var sourceCount = await CountRowsSqlServerAsync(sourceConnection, match.Source);
            var targetCount = await CountRowsPostgresAsync(targetConnection, match.Target);

            if (sourceCount != targetCount)
            {
                mismatches.Add(new ValidationMismatch(match.Source, sourceCount, targetCount));
            }
        }

        CliConsole.RenderValidationSummary(comparedTableCount, mismatches);

        if (mismatches.Count > 0)
        {
            CliConsole.Error("Validation failed: row count mismatches found.");

            return -1;
        }

        CliConsole.Success("Validation passed.");
        return 0;
    }

    private static async Task<long> CopyTableAsync(SqlConnection sourceConnection, NpgsqlConnection targetConnection, TableRef sourceTable, TableRef targetTable)
    {
        var sourceColumns = await SchemaDiscovery.GetSqlServerColumnsAsync(sourceConnection, sourceTable);
        var targetColumns = await SchemaDiscovery.GetPostgresColumnsAsync(targetConnection, targetTable);

        var targetLookup = targetColumns.ToDictionary(column => column, StringComparer.OrdinalIgnoreCase);
        var commonColumns = sourceColumns
            .Where(column => targetLookup.ContainsKey(column))
            .Select(column => targetLookup[column])
            .ToList();

        if (commonColumns.Count == 0)
        {
            throw new InvalidOperationException($"No common columns found for source table {sourceTable} and target table {targetTable}.");
        }

        var selectColumns = string.Join(", ", sourceColumns.Where(column => targetLookup.ContainsKey(column)).Select(SchemaDiscovery.QuoteSqlServerIdentifier));
        var insertColumns = string.Join(", ", commonColumns.Select(SchemaDiscovery.QuotePostgresIdentifier));
        var parameterList = string.Join(", ", Enumerable.Range(0, commonColumns.Count).Select(index => $"@p{index}"));

        var selectSql = $"SELECT {selectColumns} FROM {SchemaDiscovery.QualifySqlServerTable(sourceTable)};";
        var insertSql = $"INSERT INTO {SchemaDiscovery.QualifyPostgresTable(targetTable)} ({insertColumns}) VALUES ({parameterList});";

        await using var transaction = await targetConnection.BeginTransactionAsync();
        await using var selectCommand = new SqlCommand(selectSql, sourceConnection);
        await using var reader = await selectCommand.ExecuteReaderAsync();
        await using var insertCommand = new NpgsqlCommand(insertSql, targetConnection, transaction);

        for (var index = 0; index < commonColumns.Count; index++)
        {
            insertCommand.Parameters.Add(new NpgsqlParameter($"@p{index}", DBNull.Value));
        }

        long copiedRows = 0;
        while (await reader.ReadAsync())
        {
            for (var index = 0; index < commonColumns.Count; index++)
            {
                insertCommand.Parameters[index].Value = reader.IsDBNull(index) ? DBNull.Value : reader.GetValue(index);
            }

            await insertCommand.ExecuteNonQueryAsync();
            copiedRows++;
        }

        await transaction.CommitAsync();
        return copiedRows;
    }

    private static async Task<long> CountRowsSqlServerAsync(SqlConnection sourceConnection, TableRef table)
    {
        var sql = $"SELECT COUNT_BIG(1) FROM {SchemaDiscovery.QualifySqlServerTable(table)};";
        await using var command = new SqlCommand(sql, sourceConnection);
        var value = await command.ExecuteScalarAsync();
        return Convert.ToInt64(value);
    }

    private static async Task<long> CountRowsPostgresAsync(NpgsqlConnection targetConnection, TableRef table)
    {
        var sql = $"SELECT COUNT(*) FROM {SchemaDiscovery.QualifyPostgresTable(table)};";
        await using var command = new NpgsqlCommand(sql, targetConnection);
        var value = await command.ExecuteScalarAsync();
        return Convert.ToInt64(value);
    }

    private static async Task<TableRef?> FindFirstNonEmptyPostgresTableAsync(NpgsqlConnection targetConnection, IReadOnlyCollection<TableRef> targetTables)
    {
        foreach (var table in targetTables)
        {
            var query = $"SELECT EXISTS (SELECT 1 FROM {SchemaDiscovery.QualifyPostgresTable(table)} LIMIT 1);";
            await using var command = new NpgsqlCommand(query, targetConnection);
            var exists = await command.ExecuteScalarAsync();
            if (exists is bool boolExists && boolExists)
            {
                return table;
            }
        }

        return null;
    }

    private static async Task ReseedSequenceIfPresentAsync(NpgsqlConnection targetConnection, TableRef table)
    {
        const string idColumnQuery = @"
    SELECT column_name
    FROM information_schema.columns
    WHERE table_schema = @schemaName
      AND table_name = @tableName
      AND lower(column_name) = 'id'
    LIMIT 1;";

        await using var idColumnCommand = new NpgsqlCommand(idColumnQuery, targetConnection);
        idColumnCommand.Parameters.AddWithValue("@schemaName", table.Schema);
        idColumnCommand.Parameters.AddWithValue("@tableName", table.Name);

        var idColumnValue = await idColumnCommand.ExecuteScalarAsync();
        if (idColumnValue is not string idColumnName || string.IsNullOrWhiteSpace(idColumnName))
        {
            return;
        }

        var sql = $@"
SELECT pg_get_serial_sequence(@qualifiedTable, @idColumn);";
        await using var sequenceNameCommand = new NpgsqlCommand(sql, targetConnection);
        var qualifiedTable = $"{SchemaDiscovery.QuotePostgresIdentifier(table.Schema)}.{SchemaDiscovery.QuotePostgresIdentifier(table.Name)}";
        sequenceNameCommand.Parameters.AddWithValue("@qualifiedTable", qualifiedTable);
        sequenceNameCommand.Parameters.AddWithValue("@idColumn", idColumnName);

        var sequenceNameValue = await sequenceNameCommand.ExecuteScalarAsync();
        if (sequenceNameValue is not string sequenceName || string.IsNullOrWhiteSpace(sequenceName))
        {
            return;
        }

        var reseedSql = $@"
SELECT setval(
    @sequenceName,
    COALESCE((SELECT MAX({SchemaDiscovery.QuotePostgresIdentifier(idColumnName)}) FROM {SchemaDiscovery.QualifyPostgresTable(table)}), 1),
    true
);";
        await using var reseedCommand = new NpgsqlCommand(reseedSql, targetConnection);
        reseedCommand.Parameters.AddWithValue("@sequenceName", sequenceName);
        await reseedCommand.ExecuteNonQueryAsync();
    }

    private static async Task SetPostgresSessionReplicationRoleAsync(NpgsqlConnection connection, string role)
    {
        var sql = $"SET session_replication_role = {role};";
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> DatabaseExistsAsync(NpgsqlConnection adminConnection, string databaseName)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName);";
        await using var command = new NpgsqlCommand(sql, adminConnection);
        command.Parameters.AddWithValue("@databaseName", databaseName);
        var value = await command.ExecuteScalarAsync();
        return value is true;
    }

    private static async Task DropDatabaseAsync(NpgsqlConnection adminConnection, string databaseName)
    {
        const string terminateSql = @"
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = @databaseName
  AND pid <> pg_backend_pid();";

        await using (var terminateCommand = new NpgsqlCommand(terminateSql, adminConnection))
        {
            terminateCommand.Parameters.AddWithValue("@databaseName", databaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        var dropSql = $"DROP DATABASE {SchemaDiscovery.QuotePostgresIdentifier(databaseName)};";
        await using var dropCommand = new NpgsqlCommand(dropSql, adminConnection);
        await dropCommand.ExecuteNonQueryAsync();
    }

    private static async Task CreateDatabaseAsync(NpgsqlConnection adminConnection, string databaseName)
    {
        var createSql = $"CREATE DATABASE {SchemaDiscovery.QuotePostgresIdentifier(databaseName)};";
        await using var createCommand = new NpgsqlCommand(createSql, adminConnection);
        await createCommand.ExecuteNonQueryAsync();
    }

    private static async Task CreateTargetSchemaFromSourceAsync(
        NpgsqlConnection targetConnection,
        IReadOnlyCollection<SqlServerTableDefinition> sourceDefinitions)
    {
        var schemaNames = sourceDefinitions
            .Select(definition => definition.Table.Schema)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var schemaName in schemaNames)
        {
            var createSchemaSql = $"CREATE SCHEMA IF NOT EXISTS {SchemaDiscovery.QuotePostgresIdentifier(schemaName)};";
            await using var createSchemaCommand = new NpgsqlCommand(createSchemaSql, targetConnection);
            await createSchemaCommand.ExecuteNonQueryAsync();
        }

        var fallbackMappings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedTables = new List<TableRef>();

        await AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(false)
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            ])
            .StartAsync(async context =>
            {
                var task = context.AddTask("[green]Creating PostgreSQL table structure[/]", maxValue: sourceDefinitions.Count == 0 ? 1 : sourceDefinitions.Count);
                foreach (var tableDefinition in sourceDefinitions)
                {
                    task.Description = $"[green]Creating {Markup.Escape(tableDefinition.Table.ToString())}[/]";

                    var createTableSql = BuildCreateTableSql(tableDefinition, fallbackMappings);
                    if (string.IsNullOrWhiteSpace(createTableSql))
                    {
                        skippedTables.Add(tableDefinition.Table);
                        CliConsole.Warning($"Skipped {tableDefinition.Table} because all columns are computed.");
                        task.Increment(1);
                        continue;
                    }

                    await using var createTableCommand = new NpgsqlCommand(createTableSql, targetConnection);
                    await createTableCommand.ExecuteNonQueryAsync();
                    task.Increment(1);
                }
            });

        CliConsole.Info($"Created structure for {sourceDefinitions.Count - skippedTables.Count} table(s).");

        if (skippedTables.Count > 0)
        {
            CliConsole.Warning($"Skipped {skippedTables.Count} table(s) containing only computed columns.");
        }

        if (fallbackMappings.Count > 0)
        {
            CliConsole.Warning($"Used fallback PostgreSQL type 'text' for SQL Server types: {string.Join(", ", fallbackMappings.OrderBy(type => type))}.");
        }
    }

    private static string BuildCreateTableSql(SqlServerTableDefinition tableDefinition, ISet<string> fallbackMappings)
    {
        var nonComputedColumns = tableDefinition.Columns.Where(column => !column.IsComputed).ToList();
        if (nonComputedColumns.Count == 0)
        {
            return string.Empty;
        }

        var columnDefinitions = nonComputedColumns
            .Select(column =>
            {
                var dataType = MapSqlServerTypeToPostgres(column, fallbackMappings);
                var identityClause = column.IsIdentity && SupportsIdentity(dataType)
                    ? " GENERATED BY DEFAULT AS IDENTITY"
                    : string.Empty;
                var nullableClause = column.IsNullable ? string.Empty : " NOT NULL";
                return $"{SchemaDiscovery.QuotePostgresIdentifier(column.Name)} {dataType}{identityClause}{nullableClause}";
            })
            .ToList();

        var qualifiedTableName = SchemaDiscovery.QualifyPostgresTable(tableDefinition.Table);
        var columnsSql = string.Join(",\n    ", columnDefinitions);
        return $"CREATE TABLE {qualifiedTableName} (\n    {columnsSql}\n);";
    }

    private static bool SupportsIdentity(string postgresType)
    {
        return postgresType is "smallint" or "integer" or "bigint";
    }

    private static string MapSqlServerTypeToPostgres(SqlServerColumnDefinition column, ISet<string> fallbackMappings)
    {
        var sqlType = column.DataType.ToLowerInvariant();
        return sqlType switch
        {
            "bigint" => "bigint",
            "int" => "integer",
            "smallint" => "smallint",
            "tinyint" => "smallint",
            "bit" => "boolean",
            "decimal" or "numeric" => $"numeric({column.Precision ?? 18},{column.Scale ?? 0})",
            "money" => "numeric(19,4)",
            "smallmoney" => "numeric(10,4)",
            "float" => "double precision",
            "real" => "real",
            "date" => "date",
            "time" => "time without time zone",
            "datetime" or "datetime2" or "smalldatetime" => "timestamp without time zone",
            "datetimeoffset" => "timestamp with time zone",
            "char" => BuildCharacterType(column.MaxLength, isVarying: false, isUnicode: false),
            "varchar" => BuildCharacterType(column.MaxLength, isVarying: true, isUnicode: false),
            "nchar" => BuildCharacterType(column.MaxLength, isVarying: false, isUnicode: true),
            "nvarchar" => BuildCharacterType(column.MaxLength, isVarying: true, isUnicode: true),
            "text" or "ntext" => "text",
            "uniqueidentifier" => "uuid",
            "binary" or "varbinary" or "image" or "rowversion" or "timestamp" => "bytea",
            "xml" => "xml",
            _ => RegisterFallback(sqlType, fallbackMappings)
        };
    }

    private static string BuildCharacterType(int? maxLength, bool isVarying, bool isUnicode)
    {
        if (maxLength is null || maxLength <= 0)
        {
            return "text";
        }

        var effectiveLength = maxLength.Value;
        if (effectiveLength < 0)
        {
            return "text";
        }

        if (isUnicode)
        {
            effectiveLength = Math.Max(1, effectiveLength / 2);
        }

        return isVarying
            ? $"character varying({effectiveLength})"
            : $"character({effectiveLength})";
    }

    private static string RegisterFallback(string sqlType, ISet<string> fallbackMappings)
    {
        fallbackMappings.Add(sqlType);
        return "text";
    }

    private static bool ShouldSkipTable(TableRef table)
    {
        if (table.Name.StartsWith("Hangfire", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (table.Name.Contains("ReadModel", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (table.Name.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
