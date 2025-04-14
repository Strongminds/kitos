param (
    [Parameter(Mandatory = $true)]
    [string]$targetEnvironment
)

$ErrorActionPreference = "Stop"

.$PSScriptRoot\SetupPubsubEnviroment.ps1
.$PSScriptRoot\PubsubDBMigrations.ps1

Load-Pubsub-Parameters -environmentName $targetEnvironment

#-------------------------------------------------------------
Write-Host "Running migrations"
#-------------------------------------------------------------
Run-Pubsub-DB-Migrations -newDb $false -migrationsFolder "$Env:PUBSUB_MIGRATIONS_FOLDER" -connectionString "$Env:PUBSUB_CONNECTION_STRING"