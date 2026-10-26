<#
.SYNOPSIS
Extracts the callback/resource from the ChatGPT broker authorization address and restarts the local broker.
#>
#Requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Local.Common.ps1')
$address = Read-Host 'Paste the full https://localhost:7443/connect/authorize?... browser address (input hidden)' -AsSecureString
$pointer = [IntPtr]::Zero
try {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($address)
    $settings = Read-LocalAuthorizationRequest ([Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer))
    # Only these two validated public settings are persisted, never the URL, state or PKCE challenge.
    & (Join-Path $PSScriptRoot 'Start-Local.ps1') -Client ChatGPT @settings
    Write-Host 'Callback/resource saved. Retry Connect in ChatGPT. Existing certificates are unchanged.'
}
finally {
    if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    $address.Dispose()
}
