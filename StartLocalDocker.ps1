param(
    [switch]$ResetData,
    [switch]$RebuildApiImages,
    [switch]$NoCache,
    # Host-side port that postgres is published on. Change this if port 5432 is
    # already taken on your machine (e.g. by a native PostgreSQL install).
    [int]$PostgresPort = 5432,
    [string]$KitosDbConnectionString,
    [string]$HangfireDbConnectionString,
    [string]$PubSubDbConnectionString
)

$ErrorActionPreference = "Stop"

if (-not $KitosDbConnectionString) {
    $KitosDbConnectionString = "Host=127.0.0.1;Port=$PostgresPort;Database=kitos;Username=kitos;Password=kitos"
}
if (-not $HangfireDbConnectionString) {
    $HangfireDbConnectionString = "Host=127.0.0.1;Port=$PostgresPort;Database=kitos_hangfiredb;Username=kitos;Password=kitos"
}
if (-not $PubSubDbConnectionString) {
    $PubSubDbConnectionString = "Host=127.0.0.1;Port=$PostgresPort;Database=kitos_pubsub;Username=kitos;Password=kitos"
}

# Published to the podman-compose process so docker-compose.yml's
# "${POSTGRES_HOST_PORT:-5432}:5432" mapping picks it up.
$env:POSTGRES_HOST_PORT = $PostgresPort

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

function Wait-ForContainerHealthy {
    param(
        [Parameter(Mandatory = $true)][string]$PodmanPath,
        [Parameter(Mandatory = $true)][string]$ContainerName,
        [int]$TimeoutSeconds = 60
    )

    Write-Host "Waiting for $ContainerName to become healthy"
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $status = & $PodmanPath inspect --format '{{.State.Health.Status}}' $ContainerName 2>$null
        if ($LASTEXITCODE -eq 0 -and $status -eq "healthy") {
            Write-Host "$ContainerName is healthy"
            return
        }
        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for $ContainerName to become healthy. Its init scripts (e.g. database/role creation) may not have finished, which can cause spurious 'must be owner' / 'permission denied' errors."
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
    Wait-ForContainerHealthy -PodmanPath $podman.Source -ContainerName "kitos-postgres"

    if ($ResetData) {
        Write-Host "Preparing KITOS and Hangfire databases"
        & "$PSScriptRoot\DeploymentScripts\PrepareLocalDatabase.ps1" `
            -kitosDbConnectionString $KitosDbConnectionString `
            -hangfireDbConnectionString $HangfireDbConnectionString
        if ($LASTEXITCODE -ne 0) {
            throw "PrepareLocalDatabase.ps1 failed"
        }

        Write-Host "Preparing PubSub database"
        & "$PSScriptRoot\DeploymentScripts\PrepareLocalPubSubDatabase.Postgres.ps1" `
            -pubsubDbConnectionString $PubSubDbConnectionString
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
