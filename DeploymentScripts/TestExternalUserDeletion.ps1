<#
.SYNOPSIS
Sends a source XML fixture directly to KITOS using a client certificate.
.DESCRIPTION
The certificate must be installed with its private key and trusted by KITOS.
A status 20 means durable Hangfire acceptance; inspect the pending event or failed job afterwards.
Use disposable test identities. This helper never applies a deletion.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][uri]$KitosBaseUrl,
    [Parameter(Mandatory)][string]$XmlPath,
    [Parameter(Mandatory)][string]$CertificateThumbprint,
    [switch]$Duplicate
)
$ErrorActionPreference = 'Stop'
if (!$KitosBaseUrl.IsAbsoluteUri -or $KitosBaseUrl.Scheme -ne 'https' -or $KitosBaseUrl.UserInfo -or
    $KitosBaseUrl.AbsolutePath -ne '/' -or $KitosBaseUrl.Query -or $KitosBaseUrl.Fragment) {
    throw 'Supply an HTTPS base URL without credentials, path, query or fragment.'
}
$certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint"
if (!$certificate.HasPrivateKey) { throw 'The client certificate must have a private key.' }
$payload = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $XmlPath).Path)
if ($payload.Length -gt 1MB) { throw 'XML exceeds the 1 MiB callback limit.' }
$uri = [uri]::new($KitosBaseUrl, 'api/v2/integration/beskedfordeler')
$attempts = if ($Duplicate) { 2 } else { 1 }
for ($i = 0; $i -lt $attempts; $i++) {
    $reply = Invoke-WebRequest -Method Post -Uri $uri -Certificate $certificate -ContentType 'application/xml' -Body $payload
    [xml]$xml = $reply.Content
    $status = $xml.SelectSingleNode("//*[local-name()='StatusKode']").InnerText
    if ($status -ne '20') { throw "Callback rejected delivery with protocol status $status." }
    Write-Output 'Durably accepted by Hangfire. Verify the review item or retained failed job in KITOS.'
}
