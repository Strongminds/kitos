param(
    [Parameter(Mandatory=$true)][string]$targetEnvironment
    )

# Stop on first error
$ErrorActionPreference = "Stop"

# Load helper libraries
.$PSScriptRoot\DeploymentSetup.ps1
.$PSScriptRoot\PreparePackage.ps1
.$PSScriptRoot\DeployWebsite.ps1

Setup-Environment -environmentName $targetEnvironment

Prepare-Package -environmentName $targetEnvironment -pathToArchive (Resolve-Path "$PSScriptRoot\..\WebPackage\Presentation.Web.zip")

Deploy-Website  -packageDirectory (Resolve-Path "$PSScriptRoot\..\WebPackage") `
                -msDeployUrl "$Env:MsDeployUrl" `
                -msDeployUser $Env:MsDeployUserName `
                -msDeployPassword $Env:MsDeployPassword
