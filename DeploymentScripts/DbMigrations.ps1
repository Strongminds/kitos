Function ConvertTo-SqlConnectionParts([string]$connectionString) {
    $cs = @{}
    ($connectionString -split ';') | Where-Object { $_ -match '=' } | ForEach-Object {
        $kv = $_ -split '=', 2
        $cs[$kv[0].Trim()] = $kv[1].Trim()
    }
    $server = if ($cs['Server']) { $cs['Server'] } else { $cs['Data Source'] }
    # Normalize server for sqlcmd: add tcp: prefix if no protocol is specified.
    # This replicates what the legacy 'Network Library=dbmssocn' keyword used to do.
    if ($server -and $server -notmatch '^(tcp|np|lpc):') {
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

Function Run-DB-Migrations([bool]$newDb = $false, [string]$migrationsFolder, [string]$connectionString) {
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

    if ($newDb -eq $true) {
        $repoRoot = Resolve-Path "$PSScriptRoot\.."
        $baselineSql = "$repoRoot\DeploymentScripts\Baseline.sql"
        Write-Host "New database detected - creating database and applying baseline schema from $baselineSql"
        New-SqlDatabase -connectionString $connectionString
        Invoke-KitosSqlFile -connectionString $connectionString -sqlFilePath $baselineSql
        Write-Host "Baseline schema applied"
    }

    # Use the pre-built migrations bundle (efbundle.exe) shipped as a build artifact.
    # This avoids needing source project files (.csproj) on the deployment agent.
    $bundlePath = "$Env:MigrationsBundlePath"
    if (-not $bundlePath) {
        Throw "MigrationsBundlePath environment variable is not set. Point it to the efbundle.exe artifact."
    }

    Write-Host "Running migrations bundle: $bundlePath"
    & $bundlePath --connection "$connectionString"

    if ($LASTEXITCODE -ne 0) { Throw "FAILED TO MIGRATE DB" }
}