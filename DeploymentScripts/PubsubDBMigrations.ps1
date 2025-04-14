Function Run-Pubsub-DB-Migrations(
    [string]$connectionString
) {
    Write-Host "Executing pubsub db migrations"

    $dataAccessFolder = Resolve-Path "$PSScriptRoot\..\PubSub.DataAccess"

    & dotnet ef database update `
         --project "$dataAccessFolder" `
         --connection "$connectionString"

    if ($LASTEXITCODE -ne 0) { 
        Throw "FAILED TO MIGRATE PUBSUB DB" 
    }
}
