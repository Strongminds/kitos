Function LooksLikePostgreSqlConnectionString([string]$connectionString) {
    if (-not $connectionString) { return $false }
    # SQL Server-specific keys — cannot be a PostgreSQL connection string
    if ($connectionString -imatch 'Initial Catalog=' -or $connectionString -imatch 'Data Source=') { return $false }
    # PostgreSQL-specific keys (User ID= is ambiguous; it is also used by SQL Server)
    return (
        $connectionString -imatch 'Host=' -or
        $connectionString -imatch 'Username='
    )
}

Function Get-DatabaseProvider {
    param([string]$connectionString = $null)
    # Connection string format is authoritative: a PostgreSQL-formatted connection string
    # cannot be used with SQL Server, so detect from it first.
    if ($connectionString -and (LooksLikePostgreSqlConnectionString $connectionString)) { return "PostgreSql" }
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
    $cs = @{ Host = $null; Server = $null; Port = $null; Database = $null; UserId = $null; Username = $null; Password = $null; "SSL Mode" = $null }
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
        SslMode  = $cs['SSL Mode']
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

    $sslPart = if ($parts.SslMode) { ";SSL Mode=$($parts.SslMode)" } else { "" }
    return "Host=$normalizedHost;Port=$($parts.Port);Database=$normalizedDatabase;Username=$($parts.Username);Password=$($parts.Password)$sslPart"
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

Function Run-DB-Migrations([bool]$newDb = $false, [string]$connectionString, [string]$buildConfiguration = "Release") {
    Write-Host "Executing db migrations"
    $provider = Get-DatabaseProvider -connectionString $connectionString
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

    # Verify TCP connectivity before proceeding with any migration operations.
    if ($isPostgreSql) {
        $parts = ConvertTo-PostgresConnectionParts $connectionString
        if (-not $parts.Host) {
            throw "PostgreSQL connection string must contain Host"
        }

        $pgHost = $parts.Host.Trim()
        $isLocalServer = $newDb -eq $true -or ($pgHost -match '^(\.|(\(local\))|localhost|(\(localdb\)))(\\|,|$)')
        if (-not $isLocalServer) {
            $pgPort = if ($parts.Port) { [int]$parts.Port } else { 5432 }
            Wait-ForTcpPort -Hostname $pgHost -Port $pgPort
        }
    } else {
        # Skip for local SQL Server instances — they use named pipes or shared memory, not TCP.
        $parts = ConvertTo-SqlConnectionParts $connectionString
        $rawServer = $parts.Server -replace '^tcp:', ''
        $splitParts = $rawServer -split ',', 2
        # Strip the named instance suffix (\INSTANCENAME) — only the host/IP is needed for TCP checks.
        $sqlHost = ($splitParts[0] -split '\\')[0].Trim()
        $isLocalServer = $newDb -eq $true -or ($sqlHost -match '^(\.|(\(local\))|localhost|(\(localdb\)))(\\|,|$)')
        if (-not $isLocalServer) {
            $sqlPort = if ($splitParts.Count -gt 1) { [int]$splitParts[1].Trim() } else { 1433 }
            Wait-ForTcpPort -Hostname $sqlHost -Port $sqlPort
        }
    }

    $repoRoot = Resolve-Path "$PSScriptRoot\.."
    $infraProject = "$repoRoot\Infrastructure.DataAccess\Infrastructure.DataAccess.csproj"
    # Use Infrastructure.DataAccess as startup for dotnet ef fallback.
    # This avoids loading Presentation.Web (not required because KitosContextDesignTimeFactory
    # resolves provider/connection from environment variables).
    $startupProject = $infraProject

    if ($newDb -eq $true) {
        if ($isPostgreSql) {
            Write-Host "New PostgreSQL database detected - ensuring database exists"
            New-PostgresDatabase -connectionString $connectionString
        } else {
            Write-Host "New SQL Server database detected - creating database"
            New-SqlDatabase -connectionString $connectionString
        }
    }

    # Expose the connection string via the standard .NET env var so the
    # KitosContextDesignTimeFactory can pick it up without a hardcoded fallback.
    $Env:ConnectionStrings__KitosContext = $connectionString
    if ($isPostgreSql) {
        $Env:Database__Provider = $provider
    } else {
        $Env:Database__Provider = "SqlServer"
    }
    $Env:IgnorePendingModelChangesWarning = "true"

    # CI path: use provider-specific pre-built self-contained bundle (no source or SDK required on the agent).
    # Local dev fallback: build and run via dotnet ef when the bundle is not present.
    $bundleExe = if ($isPostgreSql) {
        "$PSScriptRoot\..\MigrationsBundle\efbundle.postgresql.exe"
    } else {
        "$PSScriptRoot\..\MigrationsBundle\efbundle.exe"
    }

    $preferBundle = $buildConfiguration -eq "Release" -or $Env:UseMigrationsBundle -eq "true"
    $useBundle = $preferBundle -and (Test-Path $bundleExe)
    if ($useBundle) {
        $migrationInputPaths = @(
            "$repoRoot\Infrastructure.DataAccess\KitosContext.cs",
            "$repoRoot\Infrastructure.DataAccess\KitosContextDesignTimeFactory.cs",
            "$repoRoot\Infrastructure.DataAccess\KitosNpgsqlMigrationsSqlGenerator.cs"
        )
        $migrationFiles = Get-ChildItem -Path "$repoRoot\Infrastructure.DataAccess\Migrations\EfCore" -Filter "*.cs" -File -Recurse -ErrorAction SilentlyContinue
        $migrationInputPaths += $migrationFiles | ForEach-Object { $_.FullName }

        $newestInputUtc = [DateTime]::MinValue
        foreach ($path in $migrationInputPaths) {
            if (-not (Test-Path -LiteralPath $path)) {
                continue
            }

            $lastWrite = (Get-Item -LiteralPath $path).LastWriteTimeUtc
            if ($lastWrite -gt $newestInputUtc) {
                $newestInputUtc = $lastWrite
            }
        }

        $bundleTimestampUtc = (Get-Item -LiteralPath $bundleExe).LastWriteTimeUtc
        if ($bundleTimestampUtc -lt $newestInputUtc) {
            Write-Warning "Skipping stale migrations bundle at $bundleExe. Newer migration inputs exist ($newestInputUtc). Falling back to dotnet ef."
            $useBundle = $false
        }
    }

    if ($useBundle) {
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
    }

    if (-not $useBundle) {
        if (-not $preferBundle) {
            Write-Host "Running dotnet ef database update (bundle disabled for this configuration)"
        } else {
            Write-Host "Migrations bundle unavailable or stale, running dotnet ef database update"
        }
        dotnet ef database update `
            --project "$infraProject" `
            --startup-project "$startupProject" `
            --connection "$connectionString" `
            --configuration "$buildConfiguration"

        if ($LASTEXITCODE -ne 0) { Throw "FAILED TO MIGRATE DB" }
    }
}