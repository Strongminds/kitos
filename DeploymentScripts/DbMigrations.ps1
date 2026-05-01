Function Get-DatabaseProvider {
    if ($Env:Database__Provider) { return $Env:Database__Provider }
    return "SqlServer"
}

Function Is-PostgreSqlProvider([string]$provider) {
    return $provider -and (
        $provider.Equals("PostgreSql", [System.StringComparison]::OrdinalIgnoreCase) -or
        $provider.Equals("Postgres", [System.StringComparison]::OrdinalIgnoreCase) -or
        $provider.Equals("Npgsql", [System.StringComparison]::OrdinalIgnoreCase)
    )
}

Function ConvertTo-PostgresConnectionParts([string]$connectionString) {
    $cs = @{ Host = $null; Server = $null; Port = $null; Database = $null; UserId = $null; Username = $null; Password = $null }
    ($connectionString -split ';') | Where-Object { $_ -match '=' } | ForEach-Object {
        $kv = $_ -split '=', 2
        $cs[$kv[0].Trim()] = $kv[1].Trim()
    }

    $pgHost = if ($cs['Host']) { $cs['Host'] } else { $cs['Server'] }
    $pgPort = if ($cs['Port']) { $cs['Port'] } else { "5432" }
    $pgDatabase = $cs['Database']
    $pgUsername = if ($cs['Username']) { $cs['Username'] } else { $cs['User ID'] }

    return @{
        Host     = $pgHost
        Port     = $pgPort
        Database = $pgDatabase
        Username = $pgUsername
        Password = $cs['Password']
    }
}

Function Normalize-PostgresConnectionString([string]$connectionString) {
    $parts = ConvertTo-PostgresConnectionParts $connectionString

    if (-not $parts.Database) {
        throw "PostgreSQL connection string must contain Database"
    }

    # PostgreSQL folds unquoted identifiers to lowercase. Keep db names lowercase to avoid
    # runtime mismatches between CREATE DATABASE and subsequent connections.
    $normalizedDatabase = $parts.Database.ToLowerInvariant()
    $normalizedHost = if ($parts.Host -and $parts.Host.Equals("localhost", [System.StringComparison]::OrdinalIgnoreCase)) {
        "127.0.0.1"
    } else {
        $parts.Host
    }

    return "Host=$normalizedHost;Port=$($parts.Port);Database=$normalizedDatabase;Username=$($parts.Username);Password=$($parts.Password)"
}

Function Get-PostgresCliPath {
    $psql = Get-Command psql -ErrorAction SilentlyContinue
    if ($psql) {
        return $psql.Source
    }

    throw "PostgreSQL provider requires psql to be installed and available on PATH."
}

Function Invoke-PostgresSql([hashtable]$parts, [string]$database, [string]$sql) {
    $psqlPath = Get-PostgresCliPath
    $Env:PGPASSWORD = $parts.Password
    try {
        & $psqlPath -h $parts.Host -p $parts.Port -U $parts.Username -d $database -v ON_ERROR_STOP=1 -c $sql
        if ($LASTEXITCODE -ne 0) { throw "psql failed executing SQL" }
    } finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }
}

Function Invoke-PostgresSqlFileInternal([hashtable]$parts, [string]$sqlFilePath) {
    $normalizedSqlPath = Get-NormalizedPostgresSqlFile -sqlFilePath $sqlFilePath
    $psqlPath = Get-PostgresCliPath
    $Env:PGPASSWORD = $parts.Password
    try {
        & $psqlPath -h $parts.Host -p $parts.Port -U $parts.Username -d $parts.Database -v ON_ERROR_STOP=1 -f $normalizedSqlPath
        if ($LASTEXITCODE -ne 0) { throw "psql failed executing $normalizedSqlPath" }
    } finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        if ($normalizedSqlPath -ne $sqlFilePath) {
            Remove-Item -Path $normalizedSqlPath -ErrorAction SilentlyContinue
        }
    }
}

Function Get-NormalizedPostgresSqlFile([string]$sqlFilePath) {
    $content = Get-Content -Path $sqlFilePath -Raw
    $lines = $content -split "`r?`n"
    $seenIndexNames = @{}
    $usedFinalIndexNames = @{}

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $match = [regex]::Match($line, '^(CREATE\s+(?:UNIQUE\s+)?INDEX\s+")([^"]+)("\s+ON\s+")([^"]+)("\s*\()', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if (-not $match.Success) { continue }

        $indexName = $match.Groups[2].Value
        $tableName = $match.Groups[4].Value

        if (-not $seenIndexNames.ContainsKey($indexName)) {
            $seenIndexNames[$indexName] = 1
            $finalName = Get-UniquePostgresIdentifier -candidateName $indexName -usedNames $usedFinalIndexNames
            $usedFinalIndexNames[$finalName] = $true
            if ($finalName -ne $indexName) {
                $lines[$i] = $line.Substring(0, $match.Groups[2].Index) + $finalName + $line.Substring($match.Groups[2].Index + $match.Groups[2].Length)
            }
            continue
        }

        $seenIndexNames[$indexName] = $seenIndexNames[$indexName] + 1
        $suffix = "_${tableName}_$($seenIndexNames[$indexName])"
        $newIndexName = Get-UniquePostgresIdentifier -candidateName "$indexName$suffix" -usedNames $usedFinalIndexNames
        $usedFinalIndexNames[$newIndexName] = $true

        $lines[$i] = $line.Substring(0, $match.Groups[2].Index) + $newIndexName + $line.Substring($match.Groups[2].Index + $match.Groups[2].Length)
    }

    $normalized = [string]::Join([Environment]::NewLine, $lines)
    if ($normalized -eq $content) {
        return $sqlFilePath
    }

    $tmpFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.sql'
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($tmpFile, $normalized, $utf8NoBom)
    return $tmpFile
}

Function Get-UniquePostgresIdentifier([string]$candidateName, [hashtable]$usedNames) {
    $maxLength = 63
    $base = if ($candidateName.Length -gt $maxLength) { $candidateName.Substring(0, $maxLength) } else { $candidateName }

    if (-not $usedNames.ContainsKey($base)) {
        return $base
    }

    $counter = 2
    while ($true) {
        $suffix = "_$counter"
        $prefixLength = $maxLength - $suffix.Length
        $prefix = if ($candidateName.Length -gt $prefixLength) { $candidateName.Substring(0, $prefixLength) } else { $candidateName }
        $variant = "$prefix$suffix"
        if (-not $usedNames.ContainsKey($variant)) {
            return $variant
        }
        $counter++
    }
}

Function New-PostgresDatabase([string]$connectionString) {
    $parts = ConvertTo-PostgresConnectionParts $connectionString
    if (-not $parts.Database) { throw "PostgreSQL connection string must contain Database" }

    $escapedDatabaseNameForIdentifier = $parts.Database.Replace('"', '""')
    $escapedDatabaseNameForLiteral = $parts.Database.Replace("'", "''")
    $existsQuery = "SELECT 1 FROM pg_database WHERE datname = '$escapedDatabaseNameForLiteral'"
    $psqlPath = Get-PostgresCliPath

    $Env:PGPASSWORD = $parts.Password
    try {
        $existsOutput = (& $psqlPath -h $parts.Host -p $parts.Port -U $parts.Username -d postgres -tAc $existsQuery | Out-String)
        $exists = if ($existsOutput) { $existsOutput.Trim() } else { "" }
        if ($exists -ne "1") {
            & $psqlPath -h $parts.Host -p $parts.Port -U $parts.Username -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE `"$escapedDatabaseNameForIdentifier`""
            if ($LASTEXITCODE -ne 0) { throw "psql failed creating database $($parts.Database)" }
        }
    } finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }
}

Function Wait-ForTcpPort {
    param(
        [Parameter(Mandatory = $true)][string]$Hostname,
        [Parameter(Mandatory = $true)][int]$Port,
        [int]$MaxAttempts = 12,
        [int]$DelaySeconds = 5
    )

    for ($i = 1; $i -le $MaxAttempts; $i++) {
        Write-Host "Checking TCP connectivity to $Hostname`:$Port (attempt $i/$MaxAttempts) ..."

        $ok = Test-NetConnection -ComputerName $Hostname -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue

        if ($ok) {
            Write-Host "TCP connectivity OK ($Hostname`:$Port)"
            return
        }

        if ($i -lt $MaxAttempts) {
            Start-Sleep -Seconds $DelaySeconds
        }
    }

    throw "TCP connectivity was not established to $Hostname`:$Port after $MaxAttempts attempts."
}

Function ConvertTo-SqlConnectionParts([string]$connectionString) {
    $cs = @{}
    ($connectionString -split ';') | Where-Object { $_ -match '=' } | ForEach-Object {
        $kv = $_ -split '=', 2
        $cs[$kv[0].Trim()] = $kv[1].Trim()
    }
    $server = if ($cs['Server']) { $cs['Server'] } else { $cs['Data Source'] }
    
    # Only prepend tcp: for remote servers. Local instances (.\X, (local), localhost, (localdb)\X)
    # use named pipes or shared memory and fail when forced onto TCP.
    $isLocal = $server -match '^(\.|(\(local\))|localhost|(\(localdb\)))(\\|$)'
    if ($server -and -not $isLocal -and $server -notmatch '^(tcp|np|lpc):') {
        $server = "tcp:$server"
    }
    return @{
        Server    = $server
        Database  = if ($cs['Initial Catalog']) { $cs['Initial Catalog'] } else { $cs['Database'] }
        Trusted   = $cs['Integrated Security'] -in @('true', 'sspi', 'yes')
        TrustCert = $cs['TrustServerCertificate'] -eq 'true'
        UserId    = $cs['User ID']
        Password  = $cs['Password']
    }
}

Function Get-SqlcmdAuthArgs($parts) {
    $authArgs = @()
    if ($parts.Trusted) { $authArgs += '-E' } else { $authArgs += @('-U', $parts.UserId, '-P', $parts.Password) }
    if ($parts.TrustCert) { $authArgs += '-C' }
    return $authArgs
}

# Creates the database via master if it does not already exist.
Function New-SqlDatabase([string]$connectionString) {
    $parts = ConvertTo-SqlConnectionParts $connectionString
    $authArgs = Get-SqlcmdAuthArgs $parts
    $createSql = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$($parts.Database)') CREATE DATABASE [$($parts.Database)]"
    & sqlcmd -S $parts.Server -d master @authArgs -Q $createSql -b
    if ($LASTEXITCODE -ne 0) { Throw "sqlcmd failed creating database $($parts.Database)" }
}

# Executes a .sql file via sqlcmd, which natively handles GO batch separators.
Function Invoke-KitosSqlFile([string]$connectionString, [string]$sqlFilePath) {
    $parts = ConvertTo-SqlConnectionParts $connectionString
    $authArgs = Get-SqlcmdAuthArgs $parts
    & sqlcmd -S $parts.Server -d $parts.Database @authArgs -i $sqlFilePath -b -I
    if ($LASTEXITCODE -ne 0) { Throw "sqlcmd failed executing $sqlFilePath" }
}

# Executes a PostgreSQL .sql file through Npgsql.
Function Invoke-PostgresSqlFile([string]$connectionString, [string]$sqlFilePath) {
    if (-not (Test-Path $sqlFilePath)) {
        throw "SQL file not found: $sqlFilePath"
    }

    Write-Host "Applying PostgreSQL baseline schema from $sqlFilePath"

    $parts = ConvertTo-PostgresConnectionParts $connectionString
    Invoke-PostgresSqlFileInternal -parts $parts -sqlFilePath $sqlFilePath
}

Function Initialize-EFCoreHistoryForNewPostgresDb([string]$connectionString, [string]$migrationsPath) {
    $migrationFiles = Get-ChildItem -Path $migrationsPath -Filter "*.cs" |
        Where-Object { $_.BaseName -match '^\d+_.+' -and $_.BaseName -notlike '*Designer' } |
        Sort-Object Name

    if (-not $migrationFiles) { return }

    $historySqlBuilder = New-Object System.Text.StringBuilder
    # EF Core is configured with MigrationsHistoryTable("__EFMigrationsHistory", "dbo"), so the
    # history table must be created and populated in the dbo schema to match.
    [void]$historySqlBuilder.AppendLine('CREATE SCHEMA IF NOT EXISTS dbo;')
    [void]$historySqlBuilder.AppendLine('CREATE TABLE IF NOT EXISTS dbo."__EFMigrationsHistory" (')
    [void]$historySqlBuilder.AppendLine('    "MigrationId" character varying(150) NOT NULL,')
    [void]$historySqlBuilder.AppendLine('    "ProductVersion" character varying(32) NOT NULL,')
    [void]$historySqlBuilder.AppendLine('    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")')
    [void]$historySqlBuilder.AppendLine(');')

    # Only pre-mark the InitialBaseline migration. Post-baseline migrations run normally so that
    # new schema changes (e.g. column type conversions) are applied on every fresh database.
    $baselineMigration = $migrationFiles | Where-Object { $_.BaseName -match '_InitialBaseline$' } | Select-Object -First 1
    if ($baselineMigration) {
        $migrationId = $baselineMigration.BaseName.Replace("'", "''")
        [void]$historySqlBuilder.AppendLine("INSERT INTO dbo.`"__EFMigrationsHistory`" (`"MigrationId`", `"ProductVersion`") VALUES ('$migrationId', '10.0.6') ON CONFLICT DO NOTHING;")
    }

    $parts = ConvertTo-PostgresConnectionParts $connectionString
    Invoke-PostgresSql -parts $parts -database $parts.Database -sql $historySqlBuilder.ToString()
}

# For existing databases (previously managed by EF6), pre-marks EF Core migrations as applied
# so that dotnet ef does not attempt to re-apply schema changes that are already present.
#
# Rules:
#   - InitialBaseline: always pre-marked because the full schema already exists.
#   - AddExternalAndInternalPaymentOrganizationUnits_ToContractReadModel: pre-marked only when
#     the matching EF6 entry is found in __MigrationHistory (name match, any timestamp prefix).
Function Initialize-EFCoreHistoryForExistingDb([string]$connectionString) {
    $parts = ConvertTo-SqlConnectionParts $connectionString
    $authArgs = Get-SqlcmdAuthArgs $parts

    $sql = @"
-- Ensure the EF Core history table exists before any inserts.
-- dotnet ef creates it automatically, but we run before dotnet ef.
IF OBJECT_ID('[__EFMigrationsHistory]', 'U') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId]    nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    )
END

-- InitialBaseline: existing DB already has the full schema, never re-apply it.
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_InitialBaseline')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260413095837_InitialBaseline', '10.0.6')
    PRINT 'Pre-marked InitialBaseline'
END
"@

    $tmpFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.sql'
    # Write without BOM — PowerShell 5.x Set-Content -Encoding UTF8 emits a BOM which
    # causes sqlcmd to fail parsing the file on some versions.
    [System.IO.File]::WriteAllText($tmpFile, $sql, (New-Object System.Text.UTF8Encoding $false))
    try {
        & sqlcmd -S $parts.Server -d $parts.Database @authArgs -i $tmpFile -b
        if ($LASTEXITCODE -ne 0) { Throw "sqlcmd failed initializing EF Core migration history" }
    } finally {
        Remove-Item $tmpFile -ErrorAction SilentlyContinue
    }
}

Function Run-DB-Migrations([bool]$newDb = $false, [string]$connectionString, [string]$buildConfiguration = "Release") {
    Write-Host "Executing db migrations"
    $provider = Get-DatabaseProvider
    $isPostgreSql = Is-PostgreSqlProvider $provider
    Write-Host "Database provider: $provider"

    if ($isPostgreSql) {
        $connectionString = Normalize-PostgresConnectionString -connectionString $connectionString
        $pgParts = ConvertTo-PostgresConnectionParts $connectionString
        Write-Host "Using PostgreSQL connection target Host=$($pgParts.Host);Database=$($pgParts.Database)"
    }

    if ($newDb -eq $true) {
        Write-Host "Enabling seed for new database"
        $Env:SeedNewDb = "yes"
    } else {
        Write-Host "Disabling seed for new database"
        $Env:SeedNewDb = "no"
    }

    # Microsoft.Data.SqlClient v4+ requires explicit certificate trust for local SQL Server instances.
    # Only applied for fresh SQL Server databases - production runs on a server with a trusted certificate.
    if (-not $isPostgreSql -and $newDb -eq $true -and $connectionString -notmatch "TrustServerCertificate\s*=\s*True") {
        $connectionString = $connectionString.TrimEnd(";") + ";TrustServerCertificate=True"
    }

    # Verify TCP connectivity before proceeding with any sqlcmd or migration operations.
    # Skip for local SQL Server instances — they use named pipes or shared memory, not TCP.
    $parts = ConvertTo-SqlConnectionParts $connectionString
    $rawServer = $parts.Server -replace '^tcp:', ''
    $splitParts = $rawServer -split ',', 2
    $sqlHost = $splitParts[0].Trim()
    $isLocalServer = $newDb -eq $true -or ($sqlHost -match '^(\.|(\(local\))|localhost|(\(localdb\)))(\\|,|$)')
    if (-not $isLocalServer) {
        $sqlPort = if ($splitParts.Count -gt 1) { [int]$splitParts[1].Trim() } else { 1433 }
        Wait-ForTcpPort -Hostname $sqlHost -Port $sqlPort
    }

    $repoRoot = Resolve-Path "$PSScriptRoot\.."
    $infraProject = "$repoRoot\Infrastructure.DataAccess\Infrastructure.DataAccess.csproj"
    $startupProject = "$repoRoot\Presentation.Web\Presentation.Web.csproj"

    if ($newDb -eq $true) {
        if ($isPostgreSql) {
            Write-Host "New PostgreSQL database detected - ensuring database exists and applying EF Core baseline schema"
            New-PostgresDatabase -connectionString $connectionString

            $baselineSql = "$repoRoot\DeploymentScripts\Baseline.PostgreSql.FullModel.sql"
            Invoke-PostgresSqlFile -connectionString $connectionString -sqlFilePath $baselineSql

            $migrationsPath = "$repoRoot\Infrastructure.DataAccess\Migrations\EfCore"
            Initialize-EFCoreHistoryForNewPostgresDb -connectionString $connectionString -migrationsPath $migrationsPath
        } else {
            # New database: apply the extracted baseline SQL script which creates the full schema
            # and inserts the InitialBaseline record into __EFMigrationsHistory.
            # dotnet ef database update will then only apply migrations added after the baseline.
            $baselineSql = "$repoRoot\DeploymentScripts\Baseline.sql"
            Write-Host "New SQL Server database detected - creating database and applying baseline schema from $baselineSql"
            New-SqlDatabase -connectionString $connectionString
            Invoke-KitosSqlFile -connectionString $connectionString -sqlFilePath $baselineSql
            Write-Host "Baseline schema applied"
        }
    }

    # Expose the connection string via the standard .NET env var so the
    # KitosContextDesignTimeFactory can pick it up without a hardcoded fallback.
    $Env:ConnectionStrings__KitosContext = $connectionString
    $Env:Database__Provider = $provider
    $Env:IgnorePendingModelChangesWarning = "true"

    if (-not $isPostgreSql -and $newDb -eq $false) {
        Write-Host "Initializing EF Core migration history for existing database"
        Initialize-EFCoreHistoryForExistingDb -connectionString $connectionString
    } elseif ($isPostgreSql -and $newDb -eq $false) {
        Write-Host "Skipping SQL Server-specific EF Core history initialization for PostgreSQL"
    }

    # CI path: use provider-specific pre-built self-contained bundle (no source or SDK required on the agent).
    # Local dev fallback: build and run via dotnet ef when the bundle is not present.
    $bundleExe = if ($isPostgreSql) {
        "$PSScriptRoot\..\MigrationsBundle\efbundle.postgresql.exe"
    } else {
        "$PSScriptRoot\..\MigrationsBundle\efbundle.exe"
    }

    if (Test-Path $bundleExe) {
        Write-Host "Using pre-built migrations bundle at $bundleExe"
        $bundleArgs = @("--connection", "$connectionString")
        $verboseBundleLogging = $Env:VerboseMigrationLogging -eq "true"
        if ($verboseBundleLogging) {
            $bundleArgs += "--verbose"
        }
        $bundleOutput = & "$bundleExe" @bundleArgs 2>&1
        $bundleExitCode = $LASTEXITCODE

        if ($verboseBundleLogging) {
            $bundleOutput | ForEach-Object { Write-Host $_ }
        }
        else {
            $skipHistoryProbeLines = 0
            foreach ($line in $bundleOutput) {
                $text = "$line"

                if ($skipHistoryProbeLines -gt 0) {
                    $skipHistoryProbeLines--
                    continue
                }

                if ($text -match '^Failed executing DbCommand .*' -and $newDb -and $isPostgreSql) {
                    $skipHistoryProbeLines = 3
                    continue
                }

                Write-Host $text
            }
        }

        if ($bundleExitCode -ne 0) { Throw "FAILED TO MIGRATE DB" }
    } else {
        Write-Host "Migrations bundle not found, running dotnet ef database update"
        dotnet ef database update `
            --project "$infraProject" `
            --startup-project "$startupProject" `
            --connection "$connectionString" `
            --configuration "$buildConfiguration"

        if ($LASTEXITCODE -ne 0) { Throw "FAILED TO MIGRATE DB" }
    }
}