# Tools.MigrateSQLServer2Postgres

Console tool for copy-over migration from SQL Server to PostgreSQL.

The design goal is safety: SQL Server remains authoritative while PostgreSQL is treated as a disposable target until validation passes.

The CLI supports both scripted execution and an interactive Spectre.Console wizard with arrow-key navigation.

The tool now runs as one pipeline:

1. connection checks (SQL Server + PostgreSQL)
2. target readiness handling (create/recreate target when needed)
3. readiness checks (schema overlap and target emptiness guard)
4. data migration
5. validation

Interactive mode can:

- reuse the last saved interactive settings
- start from a local development preset for SQL Server Express and local PostgreSQL
- prompt with editable defaults before execution
- choose migration strategy before copy stage: start fresh, resume, or use current settings
- confirm overwriting an existing target database before recreation

## Usage

Interactive mode:

```powershell
dotnet run --project Tools.MigrateSQLServer2Postgres/Tools.MigrateSQLServer2Postgres.csproj -- --interactive
```

Scripted single-flow run:

```powershell
dotnet run --project Tools.MigrateSQLServer2Postgres/Tools.MigrateSQLServer2Postgres.csproj -- --source "Server=.\SQLEXPRESS;Database=Kitos;Integrated Security=true;TrustServerCertificate=true" --target "Host=127.0.0.1;Port=5432;Database=kitos;Username=postgres;Password=postgres"
```

## Flags

- `--allow-non-empty-target`: allows running against a non-empty PostgreSQL target
- `--resume`: skips tables already marked as successful
- `--continue-on-error`: keeps migrating remaining tables after a table failure during the data copy stage
- `--disable-foreign-key-checks`: sets `session_replication_role = replica` during the data copy stage
- `--interactive`: launches an interactive wizard with arrow-key navigation

Interactive settings are stored under the current user's application data folder so the next interactive run can reuse them.

## Migration journal

The data copy stage creates and updates:

- `public.__kitos_migration_journal`

This table stores per-table status (`Started`, `Succeeded`, `Failed`), timestamps, copied row counts, and error text.

## Important behaviour

- Source SQL Server data is never modified by this tool.
- Target PostgreSQL is copied into using per-table transactions.
- On a table failure, that table transaction is rolled back.
- Application cutover is not part of this tool.
- Errors, warnings, and success messages are colour-coded in the terminal.

## Current exclusions

The tool intentionally skips:

- `Hangfire*` tables
- `*ReadModel*` tables
- `__EFMigrationsHistory`

Read-model data should be rebuilt after a successful migration instead of copied directly.
