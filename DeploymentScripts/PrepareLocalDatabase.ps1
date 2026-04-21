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

$testToolsPath = Resolve-Path "$PSScriptRoot\..\Output\Tools\TestDatabase\Tools.Test.Database.exe"

if((Test-Path "$testToolsPath") -eq $false) {
    Throw "Failed to locate $testToolsPath . Please build the solution!"
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
                