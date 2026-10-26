<#
.SYNOPSIS
Starts the Podman MCP pilot for the local inspector or ChatGPT.
#>
#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Inspector', 'ChatGPT')][string]$Client = 'Inspector',
    [string]$EnvFile,
    [string]$KitosOrigin = 'https://staging.kitos.dk',
    [string]$CallbackUri,
    [string]$ResourceIdentifier,
    [switch]$NewCertificates
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Local.Common.ps1')
if ($EnvFile -and $NewCertificates) { throw 'Choose either -EnvFile or -NewCertificates.' }
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$stateRoot = Get-LocalStateRoot
$pointerName = if ($Client -eq 'Inspector') { 'local-inspector-env-path.txt' } else { 'chatgpt-env-path.txt' }
$savedEnvironment = Join-Path $stateRoot $pointerName
$previousServerCertificate = $env:LocalClient__ServerCertificatePath
$previousBrokerCertificate = $env:LocalClient__BrokerCertificatePath
$previousBrokerOrigin = $env:LocalClient__BrokerOrigin
Push-Location $workspace
try {
    Assert-LocalPrerequisites
    if ($NewCertificates -and (Test-Path -LiteralPath $savedEnvironment)) {
        $priorPath = [IO.File]::ReadAllText($savedEnvironment).Trim()
        if (Test-Path -LiteralPath $priorPath) {
            $priorSettings = Read-LocalEnvironment $priorPath $Client
            if (-not $PSBoundParameters.ContainsKey('KitosOrigin') -and $priorSettings['KITOS_ORIGIN']) { $KitosOrigin = $priorSettings['KITOS_ORIGIN'] }
            if ($Client -eq 'ChatGPT') {
                if (-not $CallbackUri) { $CallbackUri = $priorSettings['MCP_REDIRECT_URI'] }
                if (-not $ResourceIdentifier) { $ResourceIdentifier = $priorSettings['MCP_RESOURCE_IDENTIFIER'] }
            }
        }
    }
    if (-not $EnvFile -and -not $NewCertificates -and (Test-Path -LiteralPath $savedEnvironment)) {
        $EnvFile = [IO.File]::ReadAllText($savedEnvironment).Trim()
    }
    if ($EnvFile -and (($Client -eq 'Inspector' -and ($CallbackUri -or $ResourceIdentifier)) -or $PSBoundParameters.ContainsKey('KitosOrigin'))) {
        throw 'Configuration overrides require -NewCertificates or a first setup; saved environments are not silently changed.'
    }
    if ($EnvFile -and $Client -eq 'ChatGPT' -and ($CallbackUri -or $ResourceIdentifier)) {
        if ($CallbackUri) { Assert-LocalCallback $CallbackUri }
        if ($ResourceIdentifier) { Assert-LocalResource $ResourceIdentifier }
        # Validate before editing; preserve the existing certificate directory and pins.
        [void](Read-LocalEnvironment $EnvFile $Client)
        $updatedLines = @(foreach ($line in [IO.File]::ReadAllLines($EnvFile)) {
            if ($CallbackUri -and $line -match '^MCP_REDIRECT_URI=') { "MCP_REDIRECT_URI='$CallbackUri'" }
            elseif ($ResourceIdentifier -and $line -match '^MCP_RESOURCE_IDENTIFIER=') { "MCP_RESOURCE_IDENTIFIER='$ResourceIdentifier'" }
            else { $line }
        })
        [IO.File]::WriteAllLines($EnvFile, $updatedLines)
    }
    if (-not $EnvFile) {
        $parameters = @{ KitosOrigin = $KitosOrigin; ClientId = 'kitos-local-pilot'; RedirectUri = 'https://localhost:7445/callback' }
        if ($Client -eq 'ChatGPT') {
            if (-not $CallbackUri) { $CallbackUri = 'https://chatgpt.com/connector/oauth/bootstrap' }
            if (-not $ResourceIdentifier) { $ResourceIdentifier = 'https://localhost:7443/mcp' }
            Assert-LocalCallback $CallbackUri
            Assert-LocalResource $ResourceIdentifier
            $parameters.ClientId = 'kitos-chatgpt-pilot'
            $parameters.RedirectUri = $CallbackUri
            $parameters.EnableDynamicClientRegistration = $true
        }
        elseif ($CallbackUri -or $ResourceIdentifier) { throw 'CallbackUri and ResourceIdentifier apply only to ChatGPT.' }
        $EnvFile = & (Join-Path $PSScriptRoot 'Initialize-Certificates.ps1') @parameters
        if ($Client -eq 'ChatGPT') { Add-Content -LiteralPath $EnvFile -Value "MCP_RESOURCE_IDENTIFIER='$ResourceIdentifier'" }
    }
    $EnvFile = Get-LocalEnvironmentPath $Client $EnvFile
    $settings = Read-LocalEnvironment $EnvFile $Client
    Assert-LocalCertificates $settings
    [IO.Directory]::CreateDirectory($stateRoot) | Out-Null
    [IO.File]::WriteAllText($savedEnvironment, $EnvFile)
    Start-LocalPodman
    $composeArguments = @('compose', '--env-file', $EnvFile, '-f', (Join-Path $PSScriptRoot 'compose.yaml'))
    Invoke-LocalCommand 'podman' ($composeArguments + @('config', '--quiet'))
    Invoke-LocalCommand 'podman' ($composeArguments + @('up', '--build', '-d'))
    Invoke-LocalCommand 'podman' ($composeArguments + @('ps'))
    Write-Host "Environment: $EnvFile"
    Write-Host "Trust certificates in a second terminal: ./deployment/mcp-local/Invoke-Local.ps1 Trust -Client $Client"
    Write-Host "Status / stop: ./deployment/mcp-local/Invoke-Local.ps1 Status -Client $Client (or Stop)"
    if ($Client -eq 'ChatGPT') {
        if ($settings['MCP_RESOURCE_IDENTIFIER'] -eq 'https://localhost:7443/mcp' -or $settings['MCP_REDIRECT_URI'] -eq 'https://chatgpt.com/connector/oauth/bootstrap') {
            Write-Host 'Bootstrap configuration: the first ChatGPT authorization attempt may fail. Run Complete-ChatGptSetup.ps1 with that page address, then retry (see CHATGPT.md).'
        }
        Write-Host 'Next: ./deployment/mcp-local/Start-ChatGptTunnel.ps1 (prompts for tunnel details and runtime key).'
        return
    }
    $env:LocalClient__ServerCertificatePath = Join-Path $settings['MCP_SECRET_ROOT'] 'client/server.pfx'
    $env:LocalClient__BrokerCertificatePath = Join-Path $settings['MCP_SECRET_ROOT'] 'public/broker-server.pem'
    $env:LocalClient__BrokerOrigin = 'https://localhost:7443'
    Write-Host 'Inspector: https://localhost:7445. Ctrl+C stops the inspector; containers remain running.'
    Invoke-LocalCommand 'dotnet' @('run', '--project', 'Tools.Mcp.LocalClient', '-c', 'Release', '--no-launch-profile')
}
finally {
    $env:LocalClient__ServerCertificatePath = $previousServerCertificate
    $env:LocalClient__BrokerCertificatePath = $previousBrokerCertificate
    $env:LocalClient__BrokerOrigin = $previousBrokerOrigin
    Pop-Location
}
