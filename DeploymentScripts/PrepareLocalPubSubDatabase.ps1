$ErrorActionPreference = "Stop"

.$PSScriptRoot\PubsubDBMigrations.ps1

#-------------------------------------------------------------
Write-Host "Running migrations"
#-------------------------------------------------------------
Run-Pubsub-DB-Migrations -connectionString "Host=127.0.0.1;Port=5432;Database=kitos_pubsub;Username=kitos;Password=kitos"