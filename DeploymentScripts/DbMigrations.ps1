Function Invoke-KitosSql([string]$connectionString, [string]$sql) {
    # Load Microsoft.Data.SqlClient from the NuGet packages cache (guaranteed present as an EF Core dependency)
    if (-not ([System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "Microsoft.Data.SqlClient" })) {
        $sqlClientDll = Get-ChildItem "$env:USERPROFILE\.nuget\packages\microsoft.data.sqlclient" -Recurse -Filter "Microsoft.Data.SqlClient.dll" `
            | Where-Object { $_.DirectoryName -match "net8\.0|net9\.0|net10\.0" } `
            | Sort-Object FullName -Descending `
            | Select-Object -First 1
        if (-not $sqlClientDll) { Throw "Could not find Microsoft.Data.SqlClient.dll in NuGet package cache" }
        Add-Type -Path $sqlClientDll.FullName
    }
    $conn = New-Object Microsoft.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        $cmd.ExecuteNonQuery() | Out-Null
    } finally {
        $conn.Close()
    }
}

Function Run-DB-Migrations([bool]$newDb = $false, [string]$migrationsFolder, [string]$connectionString) {
    Write-Host "Executing db migrations"

    if ($newDb -eq $true) {
        Write-Host "Enabling seed for new database"
        $Env:SeedNewDb = "yes"
    } else {
        Write-Host "Disabling seed for new database"
        $Env:SeedNewDb = "no"
    }

    # Microsoft.Data.SqlClient v4+ requires explicit certificate trust for local SQL Server instances.
    # Only applied for fresh (local dev) databases - production runs on a server with a trusted certificate.
    if ($newDb -eq $true -and $connectionString -notmatch "TrustServerCertificate\s*=\s*True") {
        $connectionString = $connectionString.TrimEnd(";") + ";TrustServerCertificate=True"
    }

    $repoRoot = Resolve-Path "$PSScriptRoot\.."
    $infraProject = "$repoRoot\Infrastructure.DataAccess\Infrastructure.DataAccess.csproj"
    $startupProject = "$repoRoot\Presentation.Web\Presentation.Web.csproj"

    # On existing databases (production/staging), the schema was already built by EF6 migrations.
    # The InitialBaseline EF Core migration carries full DDL (for fresh local DBs), but must not
    # run against an existing schema. Pre-mark it as applied so dotnet ef skips the Up() body.
    if ($newDb -eq $false) {
        Write-Host "Existing database detected - pre-marking EF Core baseline migration as applied"

        $baselineMigrationId = (Get-ChildItem "$repoRoot\Infrastructure.DataAccess\Migrations\EfCore" -Filter "*_InitialBaseline.cs" `
            | Select-Object -First 1 `
            | ForEach-Object { $_.BaseName })

        $efCoreVersion = ((dotnet ef --version 2>&1) -join "" | Select-String "\d+\.\d+\.\d+").Matches[0].Value

        $createHistoryTable = "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory' AND schema_id = SCHEMA_ID('dbo')) " +
            "BEGIN " +
            "CREATE TABLE [dbo].[__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL, [ProductVersion] nvarchar(32) NOT NULL, CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])) " +
            "END"
        $insertBaseline = "IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '$baselineMigrationId') " +
            "BEGIN " +
            "INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('$baselineMigrationId', '$efCoreVersion') " +
            "END"

        Invoke-KitosSql -connectionString $connectionString -sql $createHistoryTable
        Invoke-KitosSql -connectionString $connectionString -sql $insertBaseline
        Write-Host "Baseline migration pre-applied: $baselineMigrationId"
    }

    dotnet ef database update `
        --project "$infraProject" `
        --startup-project "$startupProject" `
        --connection "$connectionString" `
        --no-build

    # NOTE: remove --no-build if you want the tool to build before migrating

    if ($LASTEXITCODE -ne 0) { Throw "FAILED TO MIGRATE DB" }
}