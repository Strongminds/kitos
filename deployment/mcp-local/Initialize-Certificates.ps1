#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$KitosOrigin,
    [Parameter(Mandatory)][string]$ClientId,
    [Parameter(Mandatory)][string]$RedirectUri,
    [switch]$EnableDynamicClientRegistration
)

$ErrorActionPreference = 'Stop'
$upstream = [Uri]$KitosOrigin
$callback = [Uri]$RedirectUri
if (-not $upstream.IsAbsoluteUri -or $upstream.Scheme -ne 'https' -or
    $upstream.AbsolutePath -ne '/' -or $upstream.UserInfo -or $upstream.Query -or $upstream.Fragment) {
    throw 'KitosOrigin must be the explicitly authorized staging HTTPS origin without a path, credentials, query or fragment.'
}
if (-not $callback.IsAbsoluteUri -or $callback.Scheme -ne 'https' -or
    $callback.UserInfo -or $callback.Fragment -or
    ($EnableDynamicClientRegistration -and ($callback.Host -cne 'chatgpt.com' -or $callback.Port -ne 443 -or
        $callback.Query -or $callback.AbsolutePath -cnotmatch '^/connector/oauth/[A-Za-z0-9]+$')) -or
    (-not $EnableDynamicClientRegistration -and $callback.Host -ne 'localhost')) {
    throw 'RedirectUri must be the exact HTTPS localhost callback, or the ChatGPT management callback when dynamic registration is enabled.'
}
if ($ClientId -notmatch '^[A-Za-z0-9_.-]{1,128}$' -or
    (@($upstream.AbsoluteUri, $callback.AbsoluteUri) | Where-Object { $_ -match "['\r\n]" })) {
    throw 'Configuration contains unsupported characters.'
}

$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = [IO.Path]::GetFullPath((Join-Path $workspace 'TestResults/mcp-local'))
$output = [IO.Path]::GetFullPath((Join-Path $testRoot ([Guid]::NewGuid().ToString('N'))))
if (-not $output.StartsWith($testRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Certificate output must remain inside the workspace TestResults/mcp-local directory.'
}
if (Test-Path -LiteralPath $output) { throw 'Refusing to overwrite existing certificate material.' }
[void][IO.Directory]::CreateDirectory($output)
# Passwordless development keys need an owner-only host directory. No trusted-store or OS-account changes.
if ($IsWindows) {
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $owner = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl.SetOwner($owner)
    $acl.SetAccessRuleProtection($true, $false)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($owner, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
    Set-Acl -LiteralPath $output -AclObject $acl
}
else {
    [IO.File]::SetUnixFileMode($output, [IO.UnixFileMode]::UserRead -bor [IO.UnixFileMode]::UserWrite -bor [IO.UnixFileMode]::UserExecute)
}
foreach ($directory in @('broker', 'worker', 'client', 'public')) {
    [void][IO.Directory]::CreateDirectory((Join-Path $output $directory))
}

function New-LocalCertificate([string]$Name, [string]$RelativePath, [string[]]$DnsNames, [string]$Purpose) {
    $rsa = [Security.Cryptography.RSA]::Create(3072)
    $certificate = $null
    try {
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            "CN=$Name", $rsa, [Security.Cryptography.HashAlgorithmName]::SHA256,
            [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false, $false, 0, $true))
        $usage = [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature
        if ($Purpose -in @('server', 'encryption')) {
            $usage = $usage -bor [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyEncipherment
        }
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new($usage, $true))
        if ($Purpose -in @('server', 'client')) {
            $oids = [Security.Cryptography.OidCollection]::new()
            $oid = if ($Purpose -eq 'server') { '1.3.6.1.5.5.7.3.1' } else { '1.3.6.1.5.5.7.3.2' }
            [void]$oids.Add([Security.Cryptography.Oid]::new($oid))
            $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oids, $true))
        }
        if ($DnsNames.Count) {
            $san = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
            foreach ($dnsName in $DnsNames) { $san.AddDnsName($dnsName) }
            $request.CertificateExtensions.Add($san.Build())
        }
        $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-5), [DateTimeOffset]::UtcNow.AddDays(7))
        $path = Join-Path $output $RelativePath
        [IO.File]::WriteAllBytes($path, $certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx, [string]::Empty))
        if ($Purpose -eq 'server') {
            [IO.File]::WriteAllText((Join-Path $output "public/$Name.pem"), $certificate.ExportCertificatePem())
        }
        return $certificate.GetCertHashString([Security.Cryptography.HashAlgorithmName]::SHA256)
    }
    finally {
        if ($certificate) { $certificate.Dispose() }
        $rsa.Dispose()
    }
}

$brokerServer = New-LocalCertificate 'broker-server' 'broker/server.pfx' @('localhost', 'broker') 'server'
$brokerWorkload = New-LocalCertificate 'broker-workload' 'broker/workload.pfx' @() 'client'
$workerServer = New-LocalCertificate 'worker-server' 'worker/server.pfx' @('worker') 'server'
$workerClient = New-LocalCertificate 'worker-client' 'worker/client.pfx' @() 'client'
$null = New-LocalCertificate 'broker-signing' 'broker/signing.pfx' @() 'signing'
$null = New-LocalCertificate 'broker-encryption' 'broker/encryption.pfx' @() 'encryption'
$null = New-LocalCertificate 'client-server' 'client/server.pfx' @('localhost') 'server'
$secretRoot = $output.Replace('\', '/')
$environment = @(
    "MCP_SECRET_ROOT='$secretRoot'"
    "KITOS_ORIGIN='$($upstream.AbsoluteUri.TrimEnd('/'))'"
    "MCP_CLIENT_ID='$ClientId'"
    "MCP_REDIRECT_URI='$($callback.AbsoluteUri)'"
    "MCP_ENABLE_DYNAMIC_CLIENT_REGISTRATION='$($EnableDynamicClientRegistration.IsPresent.ToString().ToLowerInvariant())'"
    "MCP_LOCAL_CLIENT_CERTIFICATE_PATH='$secretRoot/client/server.pfx'"
    "BROKER_SERVER_SHA256='$brokerServer'"
    "BROKER_WORKLOAD_SHA256='$brokerWorkload'"
    "WORKER_SERVER_SHA256='$workerServer'"
    "WORKER_CLIENT_SHA256='$workerClient'"
)
$environmentPath = Join-Path $output '.env'
[IO.File]::WriteAllLines($environmentPath, $environment)
Write-Output $environmentPath
