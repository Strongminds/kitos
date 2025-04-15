Function Run-Pubsub-DB-Migrations(
    [string]$connectionString
) {
    Write-Host "Executing pubsub db migrations"

    $dataAccessFolder = Resolve-Path "$PSScriptRoot\..\PubSub.DataAccess"

    # Capture both standard output and error
    $output = & dotnet ef database update `
         --project "$dataAccessFolder" `
         --connection "$connectionString" `
         --verbose 2>&1

    Write-Host "dotnet ef output:" $output

    if ($LASTEXITCODE -ne 0) { 
        Write-Error "Migration failed with exit code $LASTEXITCODE. See output above."
        Throw "FAILED TO MIGRATE PUBSUB DB" 
    }
}
