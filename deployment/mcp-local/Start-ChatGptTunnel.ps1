#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^tunnel_[A-Za-z0-9]+$')][string]$TunnelId,
    [Parameter(Mandatory)][string]$TunnelClientPath,
    [Parameter(Mandatory)][string]$BrokerCertificatePath
)

$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$clientPath = (Resolve-Path -LiteralPath $TunnelClientPath).Path
$certificatePath = (Resolve-Path -LiteralPath $BrokerCertificatePath).Path
$profileRoot = Join-Path $workspace 'TestResults/W01ChatGPT/profiles'
New-Item -ItemType Directory -Path $profileRoot -Force | Out-Null

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
