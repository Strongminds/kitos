$ErrorActionPreference = "Stop"

.$PSScriptRoot\SetupPubsubEnviroment.ps1
.$PSScriptRoot\PubsubDBMigrations.ps1

Load-Pubsub-Parameters -environmentName "INSERT_ENVIRONMENT_VARIABLE_HERE"

#-------------------------------------------------------------
Write-Host "Running migrations"
#-------------------------------------------------------------
Run-Pubsub-DB-Migrations -newDb $false -migrationsFolder "$Env:PubsubMigrationsFolder" -connectionString "$Env:PubsubDbConnectionStringForTeamCity"