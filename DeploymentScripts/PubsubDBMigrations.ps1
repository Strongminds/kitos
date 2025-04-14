Function Run-Pubsub-DB-Migrations([bool]$newDb = $false, [string]$migrationsFolder, [string]$connectionString) {
    Write-Host "Executing pubsub db migrations"

    if($newDb -eq $true) {
        Write-Host "Enabling seed for new database"
    } else {
        Write-Host "Disabling seed for new database"
    }

    & "$migrationsFolder\ef6.exe"  `
        database update  `
        --assembly "$migrationsFolder\Infrastructure.DataAccess.dll"  `
        --connection-string "$connectionString"  `
        --connection-provider "System.Data.SqlClient"  `
        --project-dir "$migrationsFolder"
    
    if($LASTEXITCODE -ne 0)	{ Throw "FAILED TO MIGRATE PUBSUB DB" }
}