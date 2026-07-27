param(
    [switch]$ResetData,
    [switch]$RebuildApiImages,
    [switch]$NoCache,
    [string]$KitosDbConnectionString = "Host=localhost;Port=5432;Database=kitos;Username=kitos;Password=kitos",
    [string]$HangfireDbConnectionString = "Host=localhost;Port=5432;Database=kitos_hangfiredb;Username=kitos;Password=kitos"
)

$ErrorActionPreference = "Stop"

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList
    )

    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $FilePath $($ArgumentList -join ' ')"
    }
}

function Invoke-NonBlockingCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList
    )

    & $FilePath @ArgumentList
}

Push-Location $PSScriptRoot
try {
    $podman = Get-Command podman -ErrorAction Stop
    Write-Host "Using podman at $($podman.Source)"

    if ($ResetData) {
        Write-Host "Resetting compose stack and volumes"
        Invoke-CheckedCommand -FilePath $podman.Source -ArgumentList @("compose", "down", "-v")
    }
    else {
        # Remove and recreate app containers to avoid port conflicts with stale containers
        Write-Host "Removing existing app containers to ensure clean startup"
        Invoke-NonBlockingCommand -FilePath $podman.Source -ArgumentList @("compose", "rm", "-sf", "kitos-api", "pubsub-api", "rabbitmq")
    }

    # Ensure only postgres runs while databases are prepared to avoid startup races.
    Write-Host "Stopping app services during database preparation"
    Invoke-NonBlockingCommand -FilePath $podman.Source -ArgumentList @("compose", "stop", "kitos-api", "pubsub-api", "rabbitmq")

    Write-Host "Starting postgres"
    Invoke-CheckedCommand -FilePath $podman.Source -ArgumentList @("compose", "up", "-d", "postgres")

    if ($ResetData) {
        Write-Host "Preparing KITOS and Hangfire databases"
        & "$PSScriptRoot\DeploymentScripts\PrepareLocalDatabase.ps1" `
            -kitosDbConnectionString $KitosDbConnectionString `
            -hangfireDbConnectionString $HangfireDbConnectionString
        if ($LASTEXITCODE -ne 0) {
            throw "PrepareLocalDatabase.ps1 failed"
        }

        Write-Host "Preparing PubSub database"
        & "$PSScriptRoot\DeploymentScripts\PrepareLocalPubSubDatabase.Postgres.ps1"
        if ($LASTEXITCODE -ne 0) {
            throw "PrepareLocalPubSubDatabase.Postgres.ps1 failed"
        }
    }
    else {
        Write-Host "Skipping database preparation"
    }

    if ($RebuildApiImages) {
        $buildArgs = @("compose", "build")
        if ($NoCache) {
            $buildArgs += "--no-cache"
        }
        $buildArgs += @("kitos-api", "pubsub-api")

        Write-Host "Rebuilding API images"
        Invoke-CheckedCommand -FilePath $podman.Source -ArgumentList $buildArgs
    }

    Write-Host "Starting application services"
    Invoke-CheckedCommand -FilePath $podman.Source -ArgumentList @("compose", "up", "-d", "rabbitmq", "kitos-api", "pubsub-api")

    Write-Host "KITOS is starting on http://localhost:5000"
}
finally {
    Pop-Location
}
