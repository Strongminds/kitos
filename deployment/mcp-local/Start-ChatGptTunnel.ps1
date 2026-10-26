#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidatePattern('^tunnel_[A-Za-z0-9]+$')][string]$TunnelId,
    [string]$TunnelClientPath,
    [string]$BrokerCertificatePath,
    [switch]$Stop,
    [switch]$Status
)

$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'Local.Common.ps1')
if ($Stop -and $Status) { throw 'Choose either -Stop or -Status.' }
$settingsPath = Join-Path (Get-LocalStateRoot) 'chatgpt-tunnel-settings.json'
if (Test-Path -LiteralPath $settingsPath) {
    $saved = [IO.File]::ReadAllText($settingsPath) | ConvertFrom-Json
    if (-not $TunnelId) { $TunnelId = $saved.TunnelId }
    if (-not $TunnelClientPath -and (Test-Path -LiteralPath $saved.TunnelClientPath)) { $TunnelClientPath = $saved.TunnelClientPath }
}
if (-not $TunnelClientPath) {
    $TunnelClientPath = Find-LocalTunnelClient
    if (-not $TunnelClientPath) { $TunnelClientPath = Read-Host 'Path to extracted tunnel-client.exe (download from Platform tunnel settings)' }
}
$TunnelClientPath = $TunnelClientPath.Trim().Trim('"')
$clientPath = (Resolve-Path -LiteralPath $TunnelClientPath).Path
Write-Host "Tunnel client: $clientPath"
if ($Stop -or $Status) {
    $operation = if ($Stop) { 'stop' } else { 'status' }
    Invoke-LocalCommand $clientPath @('runtimes', $operation, 'kitos-w01')
    return
}
if (-not $TunnelId) { $TunnelId = Read-Host 'Tunnel ID from Platform tunnel settings' }
if ($TunnelId -notmatch '^tunnel_[A-Za-z0-9]+$') { throw 'Expected a tunnel_ identifier.' }
if (-not $BrokerCertificatePath) {
    $environment = Read-LocalEnvironment (Get-LocalEnvironmentPath 'ChatGPT' '') 'ChatGPT'
    Assert-LocalCertificates $environment
    $BrokerCertificatePath = Join-Path $environment['MCP_SECRET_ROOT'] 'public/broker-server.pem'
}
$certificatePath = (Resolve-Path -LiteralPath $BrokerCertificatePath).Path
$profileRoot = Join-Path $workspace 'TestResults/W01ChatGPT/profiles'
New-Item -ItemType Directory -Path $profileRoot -Force | Out-Null
[IO.Directory]::CreateDirectory((Get-LocalStateRoot)) | Out-Null
@{ TunnelId = $TunnelId; TunnelClientPath = $clientPath } | ConvertTo-Json | Set-Content -LiteralPath $settingsPath

Write-Host 'Enter an OpenAI runtime API key with Tunnels Read + Use. Keep this key out of chat.'
$runtimeKey = Read-Host 'Runtime API key' -AsSecureString
if ($runtimeKey.Length -eq 0) { throw 'A runtime API key is required.' }
$secretPointer = [IntPtr]::Zero
$previousKey = $env:KITOS_W01_TUNNEL_RUNTIME_KEY
$previousCa = $env:CA_BUNDLE
try {
    $secretPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($runtimeKey)
    $env:KITOS_W01_TUNNEL_RUNTIME_KEY = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($secretPointer)
    # Add the public broker certificate as a trust anchor; hostname and certificate verification remain enabled.
    $env:CA_BUNDLE = $certificatePath
    & $clientPath runtimes connect --alias kitos-w01 --profile kitos-w01 --profile-dir $profileRoot `
        --tunnel-id $TunnelId --mcp-server-url 'https://localhost:7443/mcp' `
        --runtime-api-key 'env:KITOS_W01_TUNNEL_RUNTIME_KEY'
    if ($LASTEXITCODE -ne 0) { throw 'Tunnel startup failed. Share only the redacted error category.' }
    & $clientPath runtimes status kitos-w01
    if ($LASTEXITCODE -ne 0) { throw 'Tunnel runtime status failed.' }
}
finally {
    $env:KITOS_W01_TUNNEL_RUNTIME_KEY = $previousKey
    $env:CA_BUNDLE = $previousCa
    if ($secretPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secretPointer) }
    $runtimeKey.Dispose()
}
