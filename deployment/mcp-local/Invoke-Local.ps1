<#
.SYNOPSIS
Checks prerequisites, manages local certificates/services, or runs developer tests.
#>
#Requires -Version 7.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Position = 0, Mandatory)]
    [ValidateSet('Check', 'Status', 'Stop', 'Trust', 'Untrust', 'BrowserTests', 'UnitTests')][string]$Action,
    [ValidateSet('Inspector', 'ChatGPT')][string]$Client = 'Inspector',
    [string]$EnvFile
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Local.Common.ps1')
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$previousNodePath = $env:NODE_PATH
Push-Location $workspace
try {
    if ($Action -eq 'Check') {
        Assert-LocalPrerequisites
        Write-Host 'PASS: PowerShell 7, .NET 10 SDK and Podman Compose are available. Startup initializes/starts the Podman machine if needed.'
        return
    }
    if ($Action -eq 'UnitTests') {
        foreach ($project in @('Mcp', 'Mcp.ReadBoundary', 'Mcp.Worker', 'Mcp.Broker', 'Mcp.LocalClient')) {
            Invoke-LocalCommand 'dotnet' @('test', "Tests.Unit.$project")
        }
        return
    }
    if ($Action -eq 'BrowserTests') {
        if ($Client -ne 'Inspector') { throw 'BrowserTests uses the local inspector; use the ChatGPT walkthrough for a ChatGPT test.' }
        # npm.ps1 reparses invocation text in some Node distributions and breaks splatted arguments.
        $npmCommand = if ($IsWindows) { 'npm.cmd' } else { 'npm' }
        foreach ($command in @('node', $npmCommand)) {
            if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "Missing $command. Install Node.js LTS, then reopen PowerShell." }
        }
        $toolsRoot = Join-Path (Get-LocalStateRoot) 'browser-tools'
        if (-not (Test-Path (Join-Path $toolsRoot 'node_modules/playwright/package.json'))) {
            Invoke-LocalCommand $npmCommand @('install', '--prefix', $toolsRoot, '--no-save', '--package-lock=false', 'playwright')
        }
        $env:NODE_PATH = Join-Path $toolsRoot 'node_modules'
        Invoke-LocalCommand 'node' @((Join-Path $PSScriptRoot 'verify-browser.cjs'))
        Invoke-LocalCommand 'node' @((Join-Path $PSScriptRoot 'verify-callback-policy.cjs'))
        return
    }
    $EnvFile = Get-LocalEnvironmentPath $Client $EnvFile
    $settings = Read-LocalEnvironment $EnvFile $Client
    if ($Action -in @('Trust', 'Untrust')) {
        if (-not $IsWindows) { throw 'This trust helper targets Windows CurrentUser Root; configure your browser trust manually on other platforms.' }
        if ($Action -eq 'Trust') { Assert-LocalCertificates $settings }
        $names = if ($Client -eq 'ChatGPT') { @('broker-server') } else { @('broker-server', 'client-server') }
        $store = [Security.Cryptography.X509Certificates.X509Store]::new('Root', 'CurrentUser')
        $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        try {
            foreach ($name in $names) {
                $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPem(
                    [IO.File]::ReadAllText((Join-Path $settings['MCP_SECRET_ROOT'] "public/$name.pem")))
                try {
                    if ($certificate.Subject -ne "CN=$name") { throw 'Unexpected development certificate subject.' }
                    if ($PSCmdlet.ShouldProcess("CurrentUser Root: $name ($($certificate.Thumbprint))", $Action)) {
                        if ($Action -eq 'Trust') { $store.Add($certificate) } else { $store.Remove($certificate) }
                    }
                }
                finally { $certificate.Dispose() }
            }
        }
        finally { $store.Dispose() }
        Write-Host 'Restart your browser after certificate trust changes.'
        return
    }
    $composeArguments = @('compose', '--env-file', $EnvFile, '-f', (Join-Path $PSScriptRoot 'compose.yaml'))
    if ($Action -eq 'Stop') {
        if ($PSCmdlet.ShouldProcess('Local MCP broker and worker containers (shared by both clients)', 'Stop')) {
            if ($Client -eq 'ChatGPT' -and (Test-Path (Join-Path (Get-LocalStateRoot) 'chatgpt-tunnel-settings.json'))) {
                & (Join-Path $PSScriptRoot 'Start-ChatGptTunnel.ps1') -Stop
            }
            Invoke-LocalCommand 'podman' ($composeArguments + @('down'))
        }
        return
    }
    Invoke-LocalCommand 'podman' ($composeArguments + @('ps'))
    Assert-LocalCertificates $settings
    if (-not (Get-Command curl.exe -ErrorAction SilentlyContinue)) { throw 'Status requires curl.exe (included with supported Windows versions).' }
    $certificatePath = Join-Path $settings['MCP_SECRET_ROOT'] 'public/broker-server.pem'
    $status = & curl.exe --silent --show-error --max-time 10 --cacert $certificatePath -o NUL -w '%{http_code}' https://localhost:7443/.well-known/oauth-authorization-server
    if ($LASTEXITCODE -ne 0 -or $status -ne '200') { throw 'Broker HTTPS discovery did not return HTTP 200.' }
    Write-Host 'PASS: broker HTTPS discovery returned HTTP 200. Authentication/query acceptance requires the documented browser flow.'
    if ($Client -eq 'ChatGPT' -and (Test-Path (Join-Path (Get-LocalStateRoot) 'chatgpt-tunnel-settings.json'))) {
        & (Join-Path $PSScriptRoot 'Start-ChatGptTunnel.ps1') -Status
    }
}
finally {
    $env:NODE_PATH = $previousNodePath
    Pop-Location
}
