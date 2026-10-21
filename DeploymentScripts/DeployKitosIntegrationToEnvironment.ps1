#Requires -Version 7.0
<#
.SYNOPSIS
Publishes KitosIntegration and syncs it to a separate IIS application using KITOS's AWS parameters.
.EXAMPLE
./DeploymentScripts/DeployKitosIntegrationToEnvironment.ps1 -targetEnvironment integration -iisApplication 'KitosIntegration'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('integration', 'dev', 'staging', 'production')]
    [string]$targetEnvironment,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$iisApplication,

    [string]$msDeployPath = 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe',
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\AwsApi.ps1"

if ($iisApplication.Trim().TrimEnd('/', '\') -ieq 'Default Web Site') {
    throw 'Choose a separate IIS site/application; Default Web Site is the KITOS backend.'
}
if ($iisApplication -match '[",\r\n]' -or [string]::IsNullOrWhiteSpace($iisApplication)) {
    throw 'Invalid IIS site/application name.'
}
if (-not (Test-Path -LiteralPath $msDeployPath -PathType Leaf)) {
    throw "Web Deploy was not found at $msDeployPath"
}
if (-not $Env:AwsAccessKeyId -or -not $Env:AwsSecretAccessKey) {
    throw 'Set AwsAccessKeyId and AwsSecretAccessKey as for the standard KITOS deployment.'
}

Configure-Aws -accessKeyId $Env:AwsAccessKeyId -secretAccessKey $Env:AwsSecretAccessKey
$parameters = Get-SSM-Parameters -environmentName $targetEnvironment
foreach ($name in @('MsDeployUrl', 'MsDeployUserName', 'MsDeployPassword', 'HangfireDbConnectionStringForIIsApp', 'BeskedfordelerCertificateThumbprint')) {
    if ([string]::IsNullOrWhiteSpace($parameters[$name])) {
        throw "Missing AWS parameter /kitos/$targetEnvironment/$name"
    }
}
if ($parameters['KitosDbProvider'] -and $parameters['KitosDbProvider'] -notin @('Postgres', 'PostgreSql', 'Npgsql')) {
    throw 'KitosIntegration requires the shared PostgreSQL Hangfire database.'
}
foreach ($thumbprint in ($parameters['BeskedfordelerCertificateThumbprint'] -split ',')) {
    if ($thumbprint.Trim() -notmatch '^[0-9a-fA-F]{40}$') {
        throw 'BeskedfordelerCertificateThumbprint must contain comma-separated SHA-1 thumbprints.'
    }
}

# A fresh directory prevents obsolete assemblies from earlier publishes being deployed.
$publishDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("KitosIntegration-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $publishDirectory | Out-Null
try {
    & dotnet publish "$PSScriptRoot\..\KitosIntegration\KitosIntegration.csproj" -c Release -o $publishDirectory
    if ($LASTEXITCODE -ne 0) { throw 'KitosIntegration publish failed.' }

    $settingsPath = Join-Path $publishDirectory 'appsettings.json'
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $settings.ConnectionStrings.kitos_HangfireDB = $parameters['HangfireDbConnectionStringForIIsApp']
    $settings.AppSettings.BeskedfordelerCertificateThumbprint = $parameters['BeskedfordelerCertificateThumbprint']
    $settings | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $settingsPath -Encoding utf8

    # Use the same Web Deploy transport, authentication and offline rule as KITOS.
    # Pass arguments directly: passwords must never be interpreted by cmd.exe.
    $deployArguments = @(
        '-verb:sync'
        "-source:contentPath=`"$publishDirectory`""
        "-dest:contentPath=`"$iisApplication`",computerName=`"$($parameters['MsDeployUrl'])`",userName=`"$($parameters['MsDeployUserName'])`",password=`"$($parameters['MsDeployPassword'])`",authtype=`"Basic`",includeAcls=`"False`""
        '-enableRule:AppOffline'
        '-disableLink:AppPoolExtension'
        '-disableLink:ContentExtension'
        '-disableLink:CertificateExtension'
        '-skip:objectname="dirPath",absolutepath="App_Data$"'
        '-allowUntrusted'
    )
    if ($WhatIf) { $deployArguments += '-whatif' }
    & $msDeployPath @deployArguments
    if ($LASTEXITCODE -ne 0) { throw 'KitosIntegration deployment failed.' }
}
finally {
    # Only remove the unique directory created by this invocation, including stamped secrets.
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/')
    $resolvedPublishDirectory = [System.IO.Path]::GetFullPath($publishDirectory)
    if ([System.IO.Path]::GetDirectoryName($resolvedPublishDirectory) -ne $tempRoot -or
        [System.IO.Path]::GetFileName($resolvedPublishDirectory) -notlike 'KitosIntegration-*') {
        throw 'Refusing to clean a publish directory outside the temporary directory.'
    }
    Remove-Item -LiteralPath $resolvedPublishDirectory -Recurse -Force
}
