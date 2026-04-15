Function ConvertTo-SqlConnectionParts([string]$connectionString) {
    $cs = @{}
    ($connectionString -split ';') | Where-Object { $_ -match '=' } | ForEach-Object {
        $kv = $_ -split '=', 2
        $cs[$kv[0].Trim()] = $kv[1].Trim()
    }
    return @{
        Server    = if ($cs['Server'])          { $cs['Server'] }          else { $cs['Data Source'] }
        Database  = if ($cs['Initial Catalog']) { $cs['Initial Catalog'] } else { $cs['Database'] }
        Trusted   = $cs['Integrated Security'] -in @('true', 'sspi', 'yes')
        TrustCert = $cs['TrustServerCertificate'] -eq 'true'
        UserId    = $cs['User ID']
        Password  = $cs['Password']
    }
}

Function Invoke-SqlQuery([string]$connectionString, [string]$sql) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandTimeout = 300
        $cmd.CommandText = $sql
        $cmd.ExecuteNonQuery() | Out-Null
    } finally {
        $conn.Close()
    }
}

# Creates the database via master if it does not already exist.
Function New-SqlDatabase([string]$connectionString) {
    $parts = ConvertTo-SqlConnectionParts $connectionString
    # Connect to master to create the target database
    $masterCs = $connectionString -replace "(?i)(Initial Catalog|Database)\s*=[^;]*(;|$)", "Initial Catalog=master;"
    $createSql = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$($parts.Database)') CREATE DATABASE [$($parts.Database)]"
    Write-Host "Creating database $($parts.Database) if not exists"
    Invoke-SqlQuery -connectionString $masterCs -sql $createSql
}

# Executes a .sql file by splitting on GO batch separators (sqlcmd-style).
Function Invoke-KitosSqlFile([string]$connectionString, [string]$sqlFilePath) {
    Write-Host "Executing SQL file: $sqlFilePath"
    $content = Get-Content $sqlFilePath -Raw
    # Split on GO statements (case-insensitive, on its own line)
    $batches = $content -split '(?im)^\s*GO\s*$' | Where-Object { $_.Trim() -ne '' }
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    try {
        $conn.Open()
        foreach ($batch in $batches) {
            $cmd = $conn.CreateCommand()
            $cmd.CommandTimeout = 300
            $cmd.CommandText = $batch
            $cmd.ExecuteNonQuery() | Out-Null
        }
    } finally {
        $conn.Close()
    }
    Write-Host "SQL file executed successfully"
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

    dotnet ef database update `
        --project "$infraProject" `
        --startup-project "$startupProject" `
        --connection "$connectionString" `
        --no-build

    # NOTE: remove --no-build if you want the tool to build before migrating

    if ($LASTEXITCODE -ne 0) { Throw "FAILED TO MIGRATE DB" }
}