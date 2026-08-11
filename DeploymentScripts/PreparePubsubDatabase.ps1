param (
    [Parameter(Mandatory = $true)]
    [string]$targetEnvironment,
    [string]$databaseProvider = "PostgreSql"
)

$ErrorActionPreference = "Stop"

.$PSScriptRoot\SetupPubsubEnviroment.ps1
.$PSScriptRoot\PubsubDBMigrations.ps1

Load-Pubsub-Parameters -envName $targetEnvironment

$connectionString = $Env:PUBSUB_MIGRATION_CONNECTION_STRING

#-------------------------------------------------------------
# For PostgreSQL: if the Subscriptions table already exists but __EFMigrationsHistory
# does not have the initial migration recorded (database pre-dates EF migrations),
# seed the history row so EF skips trying to CREATE TABLE again.
#-------------------------------------------------------------
if ($databaseProvider -ieq "PostgreSql" -or $databaseProvider -ieq "Postgres" -or $databaseProvider -ieq "Npgsql") {
    Write-Host "Checking if migration history needs seeding..."

    $dataAccessFolder = Resolve-Path "$PSScriptRoot\..\PubSub.Infrastructure.DataAccess"

    # Build so Npgsql.dll is present in the output folder
    & dotnet build "$dataAccessFolder" --configuration Release -verbosity:quiet
    $npgsqlDll = Get-ChildItem "$dataAccessFolder\bin\Release" -Recurse -Filter "Npgsql.dll" | Select-Object -First 1

    if ($null -ne $npgsqlDll) {
        # Load all DLLs from the same folder so Npgsql's dependencies resolve
        Get-ChildItem $npgsqlDll.DirectoryName -Filter "*.dll" | ForEach-Object {
            try { Add-Type -Path $_.FullName -ErrorAction SilentlyContinue } catch {}
        }

        $seedSql = 'CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" ("MigrationId" character varying(150) NOT NULL, "ProductVersion" character varying(32) NOT NULL, CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")); INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") SELECT ''20250409135507_InitialCreate'', ''10.0.0'' WHERE EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = ''Subscriptions'') AND NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = ''20250409135507_InitialCreate'');'

        $conn = New-Object Npgsql.NpgsqlConnection($connectionString)
        try {
            $conn.Open()
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = $seedSql
            $rows = $cmd.ExecuteNonQuery()
            if ($rows -gt 0) {
                Write-Host "Seeded initial migration record into __EFMigrationsHistory."
            } else {
                Write-Host "Migration history already up to date — no seeding needed."
            }
        } finally {
            $conn.Close()
        }
    } else {
        Write-Warning "Npgsql.dll not found after build — skipping history seed step."
    }
}

#-------------------------------------------------------------
Write-Host "Running migrations"
#-------------------------------------------------------------
Run-Pubsub-DB-Migrations -connectionString $connectionString -provider $databaseProvider