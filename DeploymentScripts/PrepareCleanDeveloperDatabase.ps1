param(
    [Parameter(Mandatory=$true)][string]$testToolsExePath,
    [Parameter(Mandatory=$true)][string]$kitosDbConnectionString,
    [Parameter(Mandatory=$true)][string]$hangfireDbConnectionString,
    [Parameter(Mandatory=$true)][string]$globalAdminUserName,
    [Parameter(Mandatory=$true)][string]$globalAdminPw,
    [Parameter(Mandatory=$true)][string]$localAdminUserName,
    [Parameter(Mandatory=$true)][string]$localAdminPw,
    [Parameter(Mandatory=$true)][string]$normalUserUserName,
    [Parameter(Mandatory=$true)][string]$normalUserPw,
    [Parameter(Mandatory=$true)][string]$apiUserUserName,
    [Parameter(Mandatory=$true)][string]$apiUserPw,
    [Parameter(Mandatory=$true)][string]$apiGlobalAdminUserName,
    [Parameter(Mandatory=$true)][string]$apiGlobalAdminPw,
    [Parameter(Mandatory=$true)][string]$systemIntegratorEmail,
    [Parameter(Mandatory=$true)][string]$systemIntegratorPw,
    [string]$buildConfiguration = "Debug",
    [switch]$stopWebHostDuringReset
    )
    
#-------------------------------------------------------------
# Stop on first error
#-------------------------------------------------------------
$ErrorActionPreference = "Stop"

#-------------------------------------------------------------
# Load helper libraries
#-------------------------------------------------------------
.$PSScriptRoot\DbMigrations.ps1

if (-not $Env:Database__Provider -and $Env:KitosDbProvider) {
    $Env:Database__Provider = $Env:KitosDbProvider
}

$databaseProvider = Get-DatabaseProvider -connectionString $kitosDbConnectionString
if (Is-PostgreSqlProvider $databaseProvider) {
    $kitosDbConnectionString = Normalize-PostgresConnectionString -connectionString $kitosDbConnectionString
    $hangfireDbConnectionString = Normalize-PostgresConnectionString -connectionString $hangfireDbConnectionString
}

$Env:Database__Provider = $databaseProvider
$Env:ConnectionStrings__KitosContext = $kitosDbConnectionString

function Stop-WebHostForDatabaseReset {
    $state = @{
        W3SvcWasRunning = $false
        W3SvcStopped = $false
    }

    $w3svc = Get-Service -Name "W3SVC" -ErrorAction SilentlyContinue
    if ($null -eq $w3svc) {
        Write-Host "W3SVC service not found. Skipping web host shutdown."
        return $state
    }

    if ($w3svc.Status -eq "Running") {
        try {
            Write-Host "Stopping W3SVC to prevent Hangfire reconnects during database drop"
            Stop-Service -Name "W3SVC" -Force -ErrorAction Stop
            $state.W3SvcWasRunning = $true
            $state.W3SvcStopped = $true
            Write-Host "Stopped W3SVC"
        }
        catch {
            Write-Warning "Failed to stop W3SVC: $($_.Exception.Message). Continuing without shutdown."
        }
    }
    else {
        Write-Host "W3SVC is already stopped"
    }

    return $state
}

function Start-WebHostAfterDatabaseReset($state) {
    if ($null -eq $state) {
        return
    }

    if ($state.W3SvcStopped -and $state.W3SvcWasRunning) {
        try {
            Write-Host "Starting W3SVC after database reset"
            Start-Service -Name "W3SVC" -ErrorAction Stop
            Write-Host "Started W3SVC"
        }
        catch {
            Write-Warning "Failed to start W3SVC: $($_.Exception.Message)"
        }
    }
}

#-------------------------------------------------------------
Write-Host "Dropping existing databases (kitos and hangfire)"
#-------------------------------------------------------------

$webHostState = $null
if ($stopWebHostDuringReset) {
    $webHostState = Stop-WebHostForDatabaseReset
}

try {
    & $testToolsExePath "DropDatabase" "$kitosDbConnectionString"
    if($LASTEXITCODE -ne 0) { Throw "FAILED TO DROP KITOS DB" }

    & $testToolsExePath "DropDatabase" "$hangfireDbConnectionString"
    if($LASTEXITCODE -ne 0) { Throw "FAILED TO DROP HANGFIRE DB" }

    if (Is-PostgreSqlProvider $databaseProvider) {
        Write-Host "Ensuring Hangfire PostgreSQL database exists after reset"
        New-PostgresDatabase -connectionString "$hangfireDbConnectionString"

        $hangfireParts = ConvertTo-PostgresConnectionParts $hangfireDbConnectionString
        $knownAppUser = if ($Env:KITOS_APP_USER) { $Env:KITOS_APP_USER } else { "kitos" }
        # Always ensure role and grant privileges regardless of username match:
        # New-PostgresDatabase creates the DB as a superuser, so the app user needs
        # explicit grants even when the connection string username matches the app user.
        Ensure-PostgresRole -parts $hangfireParts -roleName $knownAppUser
        Grant-PostgresSchemaPrivileges -parts $hangfireParts -granteeUser $knownAppUser -schemaName "hangfire"
        Grant-PostgresSchemaPrivileges -parts $hangfireParts -granteeUser $knownAppUser -schemaName "public"
    }
}
finally {
    if ($stopWebHostDuringReset) {
        Start-WebHostAfterDatabaseReset -state $webHostState
    }
}

#-------------------------------------------------------------
Write-Host "Running migrations"
#-------------------------------------------------------------
Run-DB-Migrations -newDb $true -connectionString "$kitosDbConnectionString" -buildConfiguration $buildConfiguration

##-------------------------------------------------------------
Write-Host "Creating test database"
#-------------------------------------------------------------
& $testToolsExePath "CreateCleanTestDatabase"  `
                    "$kitosDbConnectionString" `
                    "$globalAdminUserName" "$globalAdminPw"  `
                    "$localAdminUserName" "$localAdminPw"  `
                    "$normalUserUserName" "$normalUserPw"  `
                    "$apiUserUserName" "$apiUserPw"  `
                    "$apiGlobalAdminUserName" "$apiGlobalAdminPw"  `
                    "$systemIntegratorEmail" "$systemIntegratorPw"

if($LASTEXITCODE -ne 0)	{ Throw "FAILED TO CREATE TEST DATABASE" }

#-------------------------------------------------------------
Write-Host ""
Write-Host "=== DB PERMISSION DIAGNOSTICS ==="
#-------------------------------------------------------------

$kitosUser = if ($Env:KITOS_APP_USER) { $Env:KITOS_APP_USER } else { "kitos" }
$kitosParts  = ConvertTo-PostgresConnectionParts $kitosDbConnectionString
$hangfirePartsCheck = ConvertTo-PostgresConnectionParts $hangfireDbConnectionString

Write-Host "App user being checked: '$kitosUser'"
Write-Host "Kitos DB:    $($kitosParts.Database) on $($kitosParts.Host):$($kitosParts.Port)"
Write-Host "Hangfire DB: $($hangfirePartsCheck.Database) on $($hangfirePartsCheck.Host):$($hangfirePartsCheck.Port)"
Write-Host ""

# 1. Role exists and can login
Write-Host "--- [1] Role existence and login ---"
$roleCheckSql = "SELECT rolname, rolcanlogin, rolcreatedb FROM pg_roles WHERE rolname = '$kitosUser';"
Invoke-PostgresSql -parts $kitosParts -database "postgres" -sql $roleCheckSql

# 2. CONNECT on postgres maintenance DB (needed by EnsureHangfireDatabaseCreated at app startup)
Write-Host ""
Write-Host "--- [2] CONNECT privilege on postgres maintenance DB ---"
$pgConnectSql = "SELECT has_database_privilege('$kitosUser', 'postgres', 'CONNECT') AS can_connect_postgres;"
Invoke-PostgresSql -parts $kitosParts -database "postgres" -sql $pgConnectSql

# 3. Privileges on kitos DB
Write-Host ""
Write-Host "--- [3] Privileges on kitos DB '$($kitosParts.Database)' ---"
$kitosDbPrivSql = "SELECT has_database_privilege('$kitosUser', '$($kitosParts.Database)', 'CONNECT') AS connect, has_database_privilege('$kitosUser', '$($kitosParts.Database)', 'CREATE') AS create;"
Invoke-PostgresSql -parts $kitosParts -database "postgres" -sql $kitosDbPrivSql

# 4. Schema privileges in kitos DB
Write-Host ""
Write-Host "--- [4] Schema privileges in kitos DB ---"
$kitosSchemaPrivSql = @"
SELECT schema_name,
       has_schema_privilege('$kitosUser', schema_name, 'USAGE') AS usage,
       has_schema_privilege('$kitosUser', schema_name, 'CREATE') AS create
FROM information_schema.schemata
WHERE schema_name IN ('dbo', 'public');
"@
Invoke-PostgresSql -parts $kitosParts -database $kitosParts.Database -sql $kitosSchemaPrivSql

# 5. Privileges on hangfire DB
Write-Host ""
Write-Host "--- [5] Privileges on hangfire DB '$($hangfirePartsCheck.Database)' ---"
$hangfireDbPrivSql = "SELECT has_database_privilege('$kitosUser', '$($hangfirePartsCheck.Database)', 'CONNECT') AS connect, has_database_privilege('$kitosUser', '$($hangfirePartsCheck.Database)', 'CREATE') AS create;"
Invoke-PostgresSql -parts $hangfirePartsCheck -database "postgres" -sql $hangfireDbPrivSql

# 6. Schema privileges in hangfire DB
Write-Host ""
Write-Host "--- [6] Schema privileges in hangfire DB ---"
$hangfireSchemaPrivSql = @"
SELECT schema_name,
       has_schema_privilege('$kitosUser', schema_name, 'USAGE') AS usage,
       has_schema_privilege('$kitosUser', schema_name, 'CREATE') AS create
FROM information_schema.schemata
WHERE schema_name IN ('hangfire', 'public');
"@
Invoke-PostgresSql -parts $hangfirePartsCheck -database $hangfirePartsCheck.Database -sql $hangfireSchemaPrivSql

Write-Host ""
Write-Host "=== END DB PERMISSION DIAGNOSTICS ==="

#-------------------------------------------------------------
Write-Host ""
Write-Host "=== IIS APP CONNECTION STRING DIAGNOSTICS ==="
# The IIS app uses KitosDbConnectionStringForIIsApp / HangfireDbConnectionStringForIIsApp,
# which are different SSM values from the TeamCity strings used above.
# Verify these can actually connect — a mismatch here is the most likely remaining cause.
#-------------------------------------------------------------

$iisKitosCs    = $Env:KitosDbConnectionStringForIIsApp
$iisHangfireCs = $Env:HangfireDbConnectionStringForIIsApp

if ([string]::IsNullOrWhiteSpace($iisKitosCs)) {
    Write-Warning "[IIS] KitosDbConnectionStringForIIsApp is not set — skipping IIS connection checks"
} else {
    $iisKitosParts    = ConvertTo-PostgresConnectionParts $iisKitosCs
    $iisHangfireParts = ConvertTo-PostgresConnectionParts $iisHangfireCs

    Write-Host "IIS Kitos CS:    Host=$($iisKitosParts.Host) Port=$($iisKitosParts.Port) DB=$($iisKitosParts.Database) User=$($iisKitosParts.Username)"
    Write-Host "IIS Hangfire CS: Host=$($iisHangfireParts.Host) Port=$($iisHangfireParts.Port) DB=$($iisHangfireParts.Database) User=$($iisHangfireParts.Username)"
    Write-Host ""

    # Test: IIS user can connect to kitos DB
    Write-Host "--- [IIS-1] IIS kitos DB connect test ---"
    try {
        Invoke-PostgresSql -parts $iisKitosParts -database $iisKitosParts.Database -sql "SELECT current_user, current_database();"
        Write-Host "OK: IIS user can connect to kitos DB"
    } catch {
        Write-Warning "FAIL: IIS user cannot connect to kitos DB: $_"
    }

    # Test: IIS user can connect to postgres maintenance DB (needed at app startup)
    Write-Host ""
    Write-Host "--- [IIS-2] IIS user CONNECT on postgres maintenance DB ---"
    try {
        Invoke-PostgresSql -parts $iisKitosParts -database "postgres" -sql "SELECT current_user, current_database();"
        Write-Host "OK: IIS user can connect to postgres maintenance DB"
    } catch {
        Write-Warning "FAIL: IIS user cannot connect to postgres maintenance DB: $_"
    }

    # Test: IIS user can connect to hangfire DB
    Write-Host ""
    Write-Host "--- [IIS-3] IIS hangfire DB connect test ---"
    try {
        Invoke-PostgresSql -parts $iisHangfireParts -database $iisHangfireParts.Database -sql "SELECT current_user, current_database();"
        Write-Host "OK: IIS user can connect to hangfire DB"
    } catch {
        Write-Warning "FAIL: IIS user cannot connect to hangfire DB: $_"
    }

    # Test: IIS user privileges on postgres maintenance DB
    Write-Host ""
    Write-Host "--- [IIS-4] IIS user privileges ---"
    $iisUser = $iisKitosParts.Username
    $iisPrivSql = @"
SELECT
    has_database_privilege('$iisUser', 'postgres', 'CONNECT')          AS pg_connect,
    has_database_privilege('$iisUser', '$($iisKitosParts.Database)', 'CONNECT')     AS kitos_connect,
    has_database_privilege('$iisUser', '$($iisHangfireParts.Database)', 'CONNECT')  AS hangfire_connect;
"@
    try {
        Invoke-PostgresSql -parts $iisKitosParts -database "postgres" -sql $iisPrivSql
    } catch {
        Write-Warning "FAIL: Could not check IIS user privileges: $_"
    }
}

Write-Host ""
Write-Host "=== END IIS APP CONNECTION STRING DIAGNOSTICS ==="