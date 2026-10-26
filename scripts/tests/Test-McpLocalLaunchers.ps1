<#
.SYNOPSIS
Exercises local launcher workflows with simulated external commands; no containers, trust changes or credentials.
#>
#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$pilotRoot = Join-Path $workspace 'deployment/mcp-local'
. (Join-Path $pilotRoot 'Local.Common.ps1')
$stateRoot = Get-LocalStateRoot
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('kitos-launcher-test-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
$global:kitosLauncherCalls = [Collections.Generic.List[string]]::new()
$global:kitosLauncherInfoFails = $false
$global:kitosLauncherMachines = '[]'
$global:kitosLauncherComposeFails = $false
$global:kitosLauncherSdk = '10.0.100 [test SDK]'
$global:kitosLauncherAuthorizationAddress = $null
function Read-Host {
    param([string]$Prompt, [switch]$AsSecureString)
    if (-not $AsSecureString -or -not $global:kitosLauncherAuthorizationAddress) { throw 'Unexpected interactive setup question.' }
    ConvertTo-SecureString $global:kitosLauncherAuthorizationAddress -AsPlainText -Force
}
function podman {
    $global:kitosLauncherCalls.Add('podman ' + ($args -join ' '))
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'info' -and $global:kitosLauncherInfoFails) { $global:LASTEXITCODE = 1 }
    if ($args[0] -eq 'machine' -and $args[1] -eq 'list') { $global:kitosLauncherMachines }
    if ($args[0] -eq 'compose' -and $global:kitosLauncherComposeFails) { $global:LASTEXITCODE = 1 }
}
function dotnet {
    $global:kitosLauncherCalls.Add('dotnet ' + ($args -join ' '))
    $global:LASTEXITCODE = 0
    if ($args[0] -eq '--list-sdks') { $global:kitosLauncherSdk }
}
function node {
    $global:kitosLauncherCalls.Add('node ' + ($args -join ' '))
    $global:LASTEXITCODE = 0
}
function npm.cmd {
    $global:kitosLauncherCalls.Add('npm.cmd ' + ($args -join ' '))
    $global:LASTEXITCODE = 0
}
function npm {
    $global:kitosLauncherCalls.Add('npm ' + ($args -join ' '))
    $global:LASTEXITCODE = 0
}
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Expect-Failure([scriptblock]$Operation, [string]$Message) {
    $failed = $false
    try { & $Operation } catch { $failed = $true }
    Assert $failed $Message
}
$pointers = @{}
foreach ($name in @('local-inspector-env-path.txt', 'chatgpt-env-path.txt', 'chatgpt-tunnel-settings.json')) {
    $path = Join-Path $stateRoot $name
    $pointers[$path] = if (Test-Path -LiteralPath $path) { [IO.File]::ReadAllText($path) } else { $null }
}
$priorOrigin = $env:LocalClient__BrokerOrigin
$generatedCertificateRoots = [Collections.Generic.List[string]]::new()
Push-Location $workspace
try {
    $certificate = $null
    $rsa = [Security.Cryptography.RSA]::Create(2048)
    try {
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=launcher-fixture', $rsa,
            [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-1), [DateTimeOffset]::UtcNow.AddDays(1))
        [IO.Directory]::CreateDirectory((Join-Path $testRoot 'public')) | Out-Null
        foreach ($name in @('broker-server', 'client-server', 'worker-server')) {
            [IO.File]::WriteAllText((Join-Path $testRoot "public/$name.pem"), $certificate.ExportCertificatePem())
        }
    }
    finally { if ($certificate) { $certificate.Dispose() }; $rsa.Dispose() }
    $inspectorFile = Join-Path $testRoot 'inspector.env'
    $chatGptFile = Join-Path $testRoot 'chatgpt.env'
    $base = "MCP_SECRET_ROOT='$($testRoot.Replace('\', '/'))'`n"
    [IO.File]::WriteAllText($inspectorFile, $base + "MCP_CLIENT_ID='kitos-local-pilot'`nMCP_REDIRECT_URI='https://localhost:7445/callback'`nMCP_ENABLE_DYNAMIC_CLIENT_REGISTRATION='false'`n")
    [IO.File]::WriteAllText($chatGptFile, $base + "MCP_CLIENT_ID='kitos-chatgpt-pilot'`nMCP_REDIRECT_URI='https://chatgpt.com/connector/oauth/test123'`nMCP_ENABLE_DYNAMIC_CLIENT_REGISTRATION='true'`nMCP_RESOURCE_IDENTIFIER='https://example.test/tunnel/resource'`n")
    $env:LocalClient__BrokerOrigin = 'https://prior.test'
    & (Join-Path $pilotRoot 'Start-Local.ps1') -EnvFile $inspectorFile
    Assert ($global:kitosLauncherCalls[-1] -like 'dotnet run*--no-launch-profile') 'Inspector was not launched.'
    Assert ($env:LocalClient__BrokerOrigin -eq 'https://prior.test') 'Caller environment not restored.'
    $global:kitosLauncherCalls.Clear()
    $global:kitosLauncherInfoFails = $true
    & (Join-Path $pilotRoot 'Start-Local.ps1') -Client ChatGPT -EnvFile $chatGptFile
    Assert ($global:kitosLauncherCalls -contains 'podman machine init') 'First-run machine initialization missing.'
    Assert ($global:kitosLauncherCalls -contains 'podman machine start') 'Stopped machine not started.'
    Assert (-not ($global:kitosLauncherCalls -like 'dotnet run*')) 'ChatGPT started the inspector.'
    $global:kitosLauncherCalls.Clear()
    $global:kitosLauncherInfoFails = $false
    & (Join-Path $pilotRoot 'Start-Local.ps1') -Client ChatGPT
    Assert ($global:kitosLauncherCalls -like "*--env-file $chatGptFile*") 'ChatGPT settings were not reused.'
    & (Join-Path $pilotRoot 'Start-Local.ps1') -Client ChatGPT -ResourceIdentifier 'https://example.test/tunnel/exact'
    $updatedSettings = Read-LocalEnvironment $chatGptFile 'ChatGPT'
    Assert ($updatedSettings['MCP_RESOURCE_IDENTIFIER'] -eq 'https://example.test/tunnel/exact') 'Resource update was not saved.'
    Assert ($updatedSettings['MCP_SECRET_ROOT'] -eq $testRoot.Replace('\', '/')) 'Resource update replaced certificates.'
    Expect-Failure { & (Join-Path $pilotRoot 'Start-Local.ps1') -Client ChatGPT -ResourceIdentifier 'https://example.test?bad=1' } 'Invalid resource update accepted.'
    Expect-Failure { Assert-LocalResource 'https://example.test/a b' } 'Malformed resource accepted.'
    $authorizationAddress = 'https://localhost:7443/connect/authorize?client_id=kitos-chatgpt-pilot&redirect_uri=https%3A%2F%2Fchatgpt.com%2Fconnector%2Foauth%2Fnew456&resource=https%3A%2F%2Fexample.test%2Ftunnel%2Factual&state=do-not-store&code_challenge=do-not-store'
    $extracted = Read-LocalAuthorizationRequest $authorizationAddress
    Assert ($extracted.CallbackUri -eq 'https://chatgpt.com/connector/oauth/new456') 'Callback extraction failed.'
    Assert ($extracted.ResourceIdentifier -eq 'https://example.test/tunnel/actual') 'Resource extraction failed.'
    Expect-Failure { Read-LocalAuthorizationRequest 'https://chatgpt.com/plugin/share' } 'Plugin sharing link accepted.'
    Expect-Failure { Read-LocalAuthorizationRequest ($authorizationAddress + '&redirect_uri=https%3A%2F%2Fevil.test') } 'Duplicate callback accepted.'
    Expect-Failure { Read-LocalAuthorizationRequest ($authorizationAddress.Replace('kitos-chatgpt-pilot', 'other-client')) } 'Wrong client accepted.'
    Expect-Failure { Read-LocalAuthorizationRequest ($authorizationAddress.Replace('https://localhost:7443', 'https://evil.test')) } 'Wrong authorization origin accepted.'
    $global:kitosLauncherAuthorizationAddress = $authorizationAddress
    $completionOutput = & (Join-Path $pilotRoot 'Complete-ChatGptSetup.ps1') 6>&1 | Out-String
    Assert ($completionOutput -notmatch 'do-not-store|code_challenge=|state=') 'Authorization details leaked to output.'
    $completedSettings = Read-LocalEnvironment $chatGptFile 'ChatGPT'
    Assert ($completedSettings['MCP_REDIRECT_URI'] -eq $extracted.CallbackUri) 'Completion did not update callback.'
    Assert ($completedSettings['MCP_RESOURCE_IDENTIFIER'] -eq $extracted.ResourceIdentifier) 'Completion did not update resource.'
    Assert ([IO.File]::ReadAllText($chatGptFile) -notmatch 'do-not-store|code_challenge|state=') 'Authorization details persisted.'
    Assert ($completedSettings['MCP_SECRET_ROOT'] -eq $testRoot.Replace('\', '/')) 'Completion replaced certificates.'
    Expect-Failure { & (Join-Path $pilotRoot 'Start-Local.ps1') -EnvFile $chatGptFile } 'Wrong client configuration accepted.'
    $global:kitosLauncherComposeFails = $true
    $global:kitosLauncherCalls.Clear()
    Expect-Failure { & (Join-Path $pilotRoot 'Start-Local.ps1') -EnvFile $inspectorFile } 'Compose failure ignored.'
    Assert (-not ($global:kitosLauncherCalls -like 'dotnet run*')) 'Inspector started after failure.'
    $global:kitosLauncherComposeFails = $false
    $global:kitosLauncherSdk = '9.0.100 [test SDK]'
    Expect-Failure { & (Join-Path $pilotRoot 'Invoke-Local.ps1') Check } 'Missing SDK 10 accepted.'
    $global:kitosLauncherSdk = '10.0.100 [test SDK]'
    $global:kitosLauncherCalls.Clear()
    & (Join-Path $pilotRoot 'Invoke-Local.ps1') Stop -Client ChatGPT -WhatIf
    Assert ($global:kitosLauncherCalls.Count -eq 0) 'Stop -WhatIf executed external commands.'
    & (Join-Path $pilotRoot 'Invoke-Local.ps1') Stop
    Assert ($global:kitosLauncherCalls[-1] -like 'podman compose*down') 'Stop did not shut down Compose.'
    $badFile = Join-Path $testRoot 'invalid.env'
    [IO.File]::WriteAllText($badFile, [IO.File]::ReadAllText($chatGptFile) + "MCP_RESOURCE_IDENTIFIER='https://other.test'`n")
    Expect-Failure { Read-LocalEnvironment $badFile 'ChatGPT' } 'Duplicate resource accepted.'
    $tunnelPath = Join-Path $testRoot 'fake tunnel client.ps1'
    [IO.File]::WriteAllText($tunnelPath, '$global:kitosLauncherCalls.Add("tunnel " + ($args -join " ")); $global:LASTEXITCODE = 0')
    $global:kitosLauncherCalls.Clear()
    & (Join-Path $pilotRoot 'Start-ChatGptTunnel.ps1') -TunnelClientPath $tunnelPath -Status
    & (Join-Path $pilotRoot 'Start-ChatGptTunnel.ps1') -TunnelClientPath $tunnelPath -Stop
    Assert ($global:kitosLauncherCalls -contains 'tunnel runtimes status kitos-w01') 'Tunnel status command incorrect.'
    Assert ($global:kitosLauncherCalls -contains 'tunnel runtimes stop kitos-w01') 'Tunnel stop command incorrect.'
    $global:kitosLauncherCalls.Clear()
    & (Join-Path $pilotRoot 'Invoke-Local.ps1') UnitTests
    Assert (@($global:kitosLauncherCalls | Where-Object { $_ -like 'dotnet test Tests.Unit.Mcp*' }).Count -eq 5) 'UnitTests did not run all five MCP projects.'
    $priorNodePath = $env:NODE_PATH
    & (Join-Path $pilotRoot 'Invoke-Local.ps1') BrowserTests
    Assert (@($global:kitosLauncherCalls | Where-Object { $_ -like 'node *verify-*.cjs' }).Count -eq 2) 'Both browser checks were not invoked.'
    Assert ($env:NODE_PATH -eq $priorNodePath) 'Browser test environment not restored.'
    # A clean checkout can start without knowing either OAuth value, then complete setup.
    Remove-Item -LiteralPath (Join-Path $stateRoot 'chatgpt-env-path.txt')
    $global:kitosLauncherAuthorizationAddress = $null
    & (Join-Path $pilotRoot 'Start-Local.ps1') -Client ChatGPT
    $bootstrapFile = Get-LocalEnvironmentPath 'ChatGPT' ''
    $bootstrapSettings = Read-LocalEnvironment $bootstrapFile 'ChatGPT'
    $generatedCertificateRoots.Add($bootstrapSettings['MCP_SECRET_ROOT'])
    Assert ($bootstrapSettings['MCP_REDIRECT_URI'] -eq 'https://chatgpt.com/connector/oauth/bootstrap') 'First start requested unknown callback.'
    Assert ($bootstrapSettings['MCP_RESOURCE_IDENTIFIER'] -eq 'https://localhost:7443/mcp') 'First start requested unknown resource.'
    $global:kitosLauncherAuthorizationAddress = $authorizationAddress
    & (Join-Path $pilotRoot 'Complete-ChatGptSetup.ps1')
    $configuredSettings = Read-LocalEnvironment $bootstrapFile 'ChatGPT'
    Assert ($configuredSettings['MCP_REDIRECT_URI'] -eq $extracted.CallbackUri) 'First-run completion failed.'
    & (Join-Path $pilotRoot 'Start-Local.ps1') -Client ChatGPT -NewCertificates
    $renewedSettings = Read-LocalEnvironment (Get-LocalEnvironmentPath 'ChatGPT' '') 'ChatGPT'
    $generatedCertificateRoots.Add($renewedSettings['MCP_SECRET_ROOT'])
    Assert ($renewedSettings['MCP_REDIRECT_URI'] -eq $extracted.CallbackUri) 'Renewal lost callback.'
    Assert ($renewedSettings['MCP_RESOURCE_IDENTIFIER'] -eq $extracted.ResourceIdentifier) 'Renewal lost resource.'
    Write-Host 'PASS: clean bootstrap/completion/renewal, private URL extraction and rejection, profile/machine workflows, fail-fast, SDK version, environment restoration, Stop/WhatIf, tunnel controls and test wrappers. External commands simulated.'
}
finally {
    $env:LocalClient__BrokerOrigin = $priorOrigin
    foreach ($path in $pointers.Keys) {
        if ($null -ne $pointers[$path]) { [IO.File]::WriteAllText($path, $pointers[$path]) }
        elseif (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    foreach ($generatedRoot in $generatedCertificateRoots) {
        $resolvedRoot = [IO.Path]::GetFullPath($generatedRoot)
        $resolvedStateRoot = [IO.Path]::GetFullPath($stateRoot)
        if (-not $resolvedRoot.StartsWith($resolvedStateRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
            (Split-Path $resolvedRoot -Leaf) -notmatch '^[a-f0-9]{32}$') { throw 'Unexpected generated certificate cleanup path.' }
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
    # Delete only this test's freshly created, resolved temporary directory.
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedTestRoot.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path $resolvedTestRoot -Leaf) -notlike 'kitos-launcher-test-*') { throw 'Unexpected test cleanup path.' }
    Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    Pop-Location
}
