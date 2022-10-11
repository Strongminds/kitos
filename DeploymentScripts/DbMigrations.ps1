Function Run-DB-Migrations([bool]$newDb = $false, [string]$migrationsFolder, [string]$connectionString) {
    Write-Host "Executing db migrations"

    if($newDb -eq $true) {
        Write-Host "Enabling seed for new database"
        $Env:SeedNewDb="yes"
    } else {
        Write-Host "Disabling seed for new database"
        $Env:SeedNewDb="no"
    }

    & "$migrationsFolder\ef6.exe"  `
        database update  `
        --assembly "$migrationsFolder\Infrastructure.DataAccess.dll"  `
        --connection-string "$connectionString"  `
        --connection-provider "System.Data.SqlClient"  `
        --project-dir "$migrationsFolder"

    # NOTE: add the --verbose flag to get full statement output (for debugging)
    
    if($LASTEXITCODE -ne 0)	{ Throw "FAILED TO MIGRATE DB" }
}