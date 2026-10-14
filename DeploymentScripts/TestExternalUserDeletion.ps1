#requires -Version 7.6
<#
.SYNOPSIS
Adds a test FK Organisation user deletion message directly to the Hangfire PostgreSQL database.
.DESCRIPTION
Uses built KITOS assemblies to enqueue a BeskedfordelerJob without calling the HTTP endpoint.
Looks up the KITOS user UUID in the application database and uses its SSO external UUID.
Generates a fresh message ID for CVR 58271713. The user must already have an SSO identity.
The default KITOS user UUID is c6c2fd0b-4542-4646-ac73-b4ab23615c64.
The Hangfire database must already be initialized. A running beskedfordeler worker and an
organization with CVR 58271713 and FK Organisation users enabled are needed to process it.
Processing creates a pending review item; it does not apply a deletion.
.PARAMETER HangfireConnectionString
PostgreSQL connection string. Defaults to the ConnectionStrings__kitos_HangfireDB environment variable.
.PARAMETER UserUuid
KITOS user UUID to test. This is resolved to SsoUserIdentities.ExternalUuid before enqueueing.
.PARAMETER KitosConnectionString
Application PostgreSQL connection string. Defaults to ConnectionStrings__KitosContext.
.PARAMETER ApplicationDirectory
Directory containing the built or published KITOS assemblies.
.EXAMPLE
$env:ConnectionStrings__kitos_HangfireDB = (Get-Content ./Presentation.Web/appsettings.json -Raw | ConvertFrom-Json).ConnectionStrings.kitos_HangfireDB
$env:ConnectionStrings__KitosContext = (Get-Content ./Presentation.Web/appsettings.json -Raw | ConvertFrom-Json).ConnectionStrings.KitosContext
./DeploymentScripts/TestExternalUserDeletion.ps1
#>
[CmdletBinding()]
param(
    [string]$HangfireConnectionString = $env:ConnectionStrings__kitos_HangfireDB,
    [string]$ApplicationDirectory = (Join-Path $PSScriptRoot '../Presentation.Web/bin/Debug/net10.0/win-x64'),
    [guid]$UserUuid = 'c6c2fd0b-4542-4646-ac73-b4ab23615c64',
    [string]$KitosConnectionString = $env:ConnectionStrings__KitosContext,
    [switch]$Duplicate
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($HangfireConnectionString)) {
    throw 'Supply -HangfireConnectionString or set ConnectionStrings__kitos_HangfireDB.'
}
if ([string]::IsNullOrWhiteSpace($KitosConnectionString)) {
    throw 'Supply -KitosConnectionString or set ConnectionStrings__KitosContext.'
}
if ($UserUuid -eq [guid]::Empty) { throw 'Supply a non-empty KITOS -UserUuid.' }
# PowerShell does not resolve assemblies from the application's ASP.NET shared framework.
$loggingAssembly = Join-Path $ApplicationDirectory 'Microsoft.Extensions.Logging.Abstractions.dll'
if (!(Test-Path -LiteralPath $loggingAssembly)) {
    $runtime = @(& dotnet --list-runtimes) | ForEach-Object {
        if ($_ -match '^Microsoft\.AspNetCore\.App (10\.\d+\.\d+) \[(.+)\]$') {
            [pscustomobject]@{ Version = [version]$Matches[1]; Directory = Join-Path $Matches[2] $Matches[1] }
        }
    } | Sort-Object Version -Descending | Select-Object -First 1
    if (!$runtime) { throw 'Install the ASP.NET Core 10 runtime or supply a self-contained KITOS application directory.' }
    $loggingAssembly = Join-Path $runtime.Directory 'Microsoft.Extensions.Logging.Abstractions.dll'
}
[void][Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $loggingAssembly).Path)
foreach ($assembly in 'Npgsql', 'Hangfire.Core', 'Hangfire.PostgreSql', 'Infrastructure.STS.Beskedfordeler') {
    $assemblyPath = Join-Path $ApplicationDirectory "$assembly.dll"
    if (!(Test-Path -LiteralPath $assemblyPath)) {
        throw "Missing $assembly.dll. Build KITOS or supply -ApplicationDirectory with the published application directory."
    }
    [void][Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $assemblyPath).Path)
}
# The callback needs the FK Organisation identity, not the local KITOS user UUID.
$connection = [Npgsql.NpgsqlConnection]::new($KitosConnectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = 'SELECT identity."ExternalUuid"
FROM dbo."User" AS app_user
LEFT JOIN dbo."SsoUserIdentities" AS identity ON identity."User_Id" = app_user."Id"
WHERE app_user."Uuid" = @userUuid'
        [void]$command.Parameters.AddWithValue('userUuid', $UserUuid)
        $reader = $command.ExecuteReader()
        try {
            if (!$reader.Read()) { throw "KITOS user $UserUuid was not found." }
            if ($reader.IsDBNull(0)) {
                throw "KITOS user $UserUuid has no SSO identity. Import/link the FK Organisation user before running this test."
            }
            $externalUserUuid = $reader.GetGuid(0)
            if ($reader.Read()) { throw "KITOS user $UserUuid has multiple SSO identities; the test cannot choose one safely." }
            if ($externalUserUuid -eq [guid]::Empty) { throw "KITOS user $UserUuid has an empty SSO external UUID." }
        } finally {
            $reader.Dispose()
        }
    } finally {
        $command.Dispose()
    }
} finally {
    $connection.Dispose()
}
$messageId = [guid]::NewGuid().ToString()
$occurredAt = [DateTime]::UtcNow.ToString('o')
$testXml = @"
<ModtagBeskedInput xmlns="urn:oio:sts:1.0.0" xmlns:b="urn:oio:besked:kuvert:1.0" xmlns:s="urn:oio:sagdok:3.0.0">
  <b:Haendelsesbesked>
    <b:BeskedId><s:UUIDIdentifikator>$messageId</s:UUIDIdentifikator></b:BeskedId>
    <b:BeskedVersion>1.0</b:BeskedVersion>
    <b:Beskedkuvert>
      <b:Filtreringsdata>
        <b:Beskedtype><s:UUIDIdentifikator>94ef3c82-10e9-4d12-a9d9-7a7242d5ba83</s:UUIDIdentifikator></b:Beskedtype>
        <b:BeskedAnsvarligAktoer><s:UUIDIdentifikator>dddddddd-dddd-dddd-dddd-dddddddddddd</s:UUIDIdentifikator></b:BeskedAnsvarligAktoer>
        <b:TilladtModtager><s:URNIdentifikator>urn:oio:cvr-nr:58271713</s:URNIdentifikator></b:TilladtModtager>
        <b:ObjektRegistrering>
          <b:Registreringstidspunkt><s:TidsstempelDatoTid>$occurredAt</s:TidsstempelDatoTid></b:Registreringstidspunkt>
          <b:ObjektAnsvarligMyndighed><s:URNIdentifikator>urn:oio:cvr-nr:58271713</s:URNIdentifikator></b:ObjektAnsvarligMyndighed>
          <b:ObjektId><s:UUIDIdentifikator>$externalUserUuid</s:UUIDIdentifikator></b:ObjektId>
          <b:ObjektType><s:UUIDIdentifikator>2b808769-8d09-4aa3-ac28-f8ac3526010f</s:UUIDIdentifikator></b:ObjektType>
          <b:ObjektHandling><s:UUIDIdentifikator>9edb0c34-36e2-4d87-aba8-3ca7ade718ee</s:UUIDIdentifikator></b:ObjektHandling>
        </b:ObjektRegistrering>
      </b:Filtreringsdata>
      <b:Leveranceinformation />
    </b:Beskedkuvert>
    <b:Beskeddata><b:Base64 encoding="UTF-8">PGF1ZGl0IC8+</b:Base64></b:Beskeddata>
  </b:Haendelsesbesked>
</ModtagBeskedInput>
"@
$originalMessage = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($testXml))
$jobType = [Infrastructure.STS.Beskedfordeler.BeskedfordelerJob]
$job = [Hangfire.Common.Job]::new($jobType, $jobType.GetMethod('Process'),
    [object[]]@($originalMessage, [Threading.CancellationToken]::None))
$options = [Hangfire.PostgreSql.PostgreSqlStorageOptions]::new()
$options.SchemaName = 'hangfire'
$options.PrepareSchemaIfNecessary = $false
$storage = [Hangfire.PostgreSql.PostgreSqlStorage]::new($HangfireConnectionString, $options)
$client = [Hangfire.BackgroundJobClient]::new($storage)
$attempts = if ($Duplicate) { 2 } else { 1 }
for ($i = 0; $i -lt $attempts; $i++) {
    try {
        $jobId = $client.Create($job, [Hangfire.States.EnqueuedState]::new('beskedfordeler'))
    } catch {
        $cause = $_.Exception.GetBaseException()
        throw "Hangfire enqueue failed ($($cause.GetType().FullName)): $($cause.Message)"
    }
    if ([string]::IsNullOrWhiteSpace($jobId)) { throw 'Hangfire did not create the job.' }
    Write-Output "Enqueued Hangfire job $jobId on beskedfordeler (message $messageId)."
}
