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
    $args = @()
    if ($parts.Trusted) { $args += '-E' } else { $args += @('-U', $parts.UserId, '-P', $parts.Password) }
    if ($parts.TrustCert) { $args += '-C' }
    return $args
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

    if ($newDb -eq $true) {
        Write-Host "Enabling seed for new database"
        $Env:SeedNewDb = "yes"
    } else {
        Write-Host "Disabling seed for new database"
        $Env:SeedNewDb = "no"
    }

    # Microsoft.Data.SqlClient v4+ requires explicit certificate trust for local SQL Server instances.
    # Only applied for fresh (local dev) databases - production runs on a server with a trusted certificate.
    if ($newDb -eq $true -and $connectionString -notmatch "TrustServerCertificate\s*=\s*True") {
        $connectionString = $connectionString.TrimEnd(";") + ";TrustServerCertificate=True"
    }

    # Verify TCP connectivity before proceeding with any sqlcmd or migration operations.
    $parts = ConvertTo-SqlConnectionParts $connectionString
    $rawServer = $parts.Server -replace '^tcp:', ''
    $splitParts = $rawServer -split ',', 2
    $sqlHost = $splitParts[0].Trim()
    $sqlPort = if ($splitParts.Count -gt 1) { [int]$splitParts[1].Trim() } else { 1433 }
    Wait-ForTcpPort -Hostname $sqlHost -Port $sqlPort

    $repoRoot = Resolve-Path "$PSScriptRoot\.."
    $infraProject = "$repoRoot\Infrastructure.DataAccess\Infrastructure.DataAccess.csproj"
    $startupProject = "$repoRoot\Presentation.Web\Presentation.Web.csproj"

    if ($newDb -eq $true) {
        # New database: apply the extracted baseline SQL script which creates the full schema
        # and inserts the InitialBaseline record into __EFMigrationsHistory.
        # dotnet ef database update will then only apply migrations added after the baseline.
        $baselineSql = "$repoRoot\DeploymentScripts\Baseline.sql"
        Write-Host "New database detected - creating database and applying baseline schema from $baselineSql"
        New-SqlDatabase -connectionString $connectionString
        Invoke-KitosSqlFile -connectionString $connectionString -sqlFilePath $baselineSql
        Write-Host "Baseline schema applied"
    }

    # Expose the connection string via the standard .NET env var so the
    # KitosContextDesignTimeFactory can pick it up without a hardcoded fallback.
    $Env:ConnectionStrings__KitosContext = $connectionString

    if ($newDb -eq $false) {
        Write-Host "Initializing EF Core migration history for existing database"
        Initialize-EFCoreHistoryForExistingDb -connectionString $connectionString
    }

    # CI path: use the pre-built self-contained bundle (no source or SDK required on the agent).
    # Local dev fallback: build and run via dotnet ef when the bundle is not present.
    $bundleExe = "$PSScriptRoot\..\MigrationsBundle\efbundle.exe"

    if (Test-Path $bundleExe) {
        Write-Host "Using pre-built migrations bundle at $bundleExe"
        & "$bundleExe" --connection "$connectionString" --verbose
    } else {
        Write-Host "Migrations bundle not found, running dotnet ef database update"
        dotnet ef database update `
            --project "$infraProject" `
            --startup-project "$startupProject" `
            --connection "$connectionString" `
            --configuration "$buildConfiguration"
    }

    if ($LASTEXITCODE -ne 0) { Throw "FAILED TO MIGRATE DB" }
}