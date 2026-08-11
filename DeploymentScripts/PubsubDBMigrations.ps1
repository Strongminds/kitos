Function Run-Pubsub-DB-Migrations(
    [string]$connectionString,
    [string]$provider = "PostgreSql"
) {
    $dataAccessFolder = Resolve-Path "$PSScriptRoot\..\PubSub.Infrastructure.DataAccess"

    # Set the environment variables for the design-time factory
    $env:DEFAULT_CONNECTION_STRING = $connectionString
    $env:Database__Provider = $provider

    & dotnet ef database update --project "$dataAccessFolder" --connection "$connectionString"

    # Check for errors
    if ($LASTEXITCODE -ne 0) { 
        Write-Error "Migration failed with exit code $LASTEXITCODE."
        Throw "FAILED TO MIGRATE PUBSUB DB" 
    }
}
