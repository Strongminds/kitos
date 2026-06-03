param(
    [Parameter(Mandatory=$true)][string]$kitosDbConnectionString,
    [Parameter(Mandatory=$true)][string]$hangfireDbConnectionString
    )
#-------------------------------------------------------------
# Stop on first error
#-------------------------------------------------------------
$ErrorActionPreference = "Stop"

#-------------------------------------------------------------
# Load helper libraries
#-------------------------------------------------------------
.$PSScriptRoot\DbMigrations.ps1

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$testToolsProjectPath = Join-Path $repoRoot "Tools.Test.Database\Tools.Test.Database.csproj"
$testToolsOutputPath = Join-Path $repoRoot "Output\Tools\TestDatabase\Tools.Test.Database.exe"

function Get-LatestWriteTimeUtc([string[]]$paths) {
    $latest = [datetime]::MinValue

    foreach ($path in $paths) {
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $item = Get-Item -LiteralPath $path
        $candidates = if ($item.PSIsContainer) {
            Get-ChildItem -LiteralPath $path -Recurse -File
        } else {
            @($item)
        }

        foreach ($candidate in $candidates) {
            if ($candidate.LastWriteTimeUtc -gt $latest) {
                $latest = $candidate.LastWriteTimeUtc
            }
        }
    }

    return $latest
}

function Ensure-TestDatabaseToolIsFresh {
    $buildInputs = @(
        $testToolsProjectPath,
        (Join-Path $repoRoot "Tools.Test.Database"),
        (Join-Path $repoRoot "Core.DomainModel"),
        (Join-Path $repoRoot "Core.DomainServices"),
        (Join-Path $repoRoot "Infrastructure.DataAccess"),
        (Join-Path $repoRoot "DeploymentScripts\Baseline.sql")
    )

    $toolNeedsBuild = -not (Test-Path -LiteralPath $testToolsOutputPath)
    if (-not $toolNeedsBuild) {
        $latestInputWriteTime = Get-LatestWriteTimeUtc -paths $buildInputs
        $toolWriteTime = (Get-Item -LiteralPath $testToolsOutputPath).LastWriteTimeUtc
        $toolNeedsBuild = $latestInputWriteTime -gt $toolWriteTime
    }

    if (-not $toolNeedsBuild) {
        return
    }

    Write-Host "Building Tools.Test.Database because the local output is missing or stale"
    dotnet build $testToolsProjectPath
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build Tools.Test.Database"
    }
}

Ensure-TestDatabaseToolIsFresh
$testToolsPath = Resolve-Path $testToolsOutputPath

if((Test-Path "$testToolsPath") -eq $false) {
    Throw "Failed to locate $testToolsPath after build. Please investigate the Tools.Test.Database output path."
}

$localUserPassword = "localNoSecret"

.$PSScriptRoot\PrepareCleanDeveloperDatabase.ps1 `
                -testToolsExePath "$testToolsPath" `
                -kitosDbConnectionString "$kitosDbConnectionString" `
                -hangfireDbConnectionString "$hangfireDbConnectionString" `
                -globalAdminUserName "local-global-admin-user@kitos.dk" `
                -globalAdminPw "$localUserPassword" `
                -localAdminUserName "local-local-admin-user@kitos.dk" `
                -localAdminPw "$localUserPassword" `
                -normalUserUserName "local-regular-user@kitos.dk" `
                -normalUserPw "$localUserPassword" `
                -apiUserUserName "local-api-user@kitos.dk" `
                -apiUserPw "$localUserPassword" `
                -apiGlobalAdminUserName "local-api-global-admin-user@kitos.dk" `
                -apiGlobalAdminPw "$localUserPassword" `
                -systemIntegratorEmail "local-api-system-integrator-user@kitos.dk" `
                -systemIntegratorPw "$localUserPassword"
                
