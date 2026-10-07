<#
.SYNOPSIS
Publishes a test external-user deletion through PubSub and verifies KITOS ingestion.
.DESCRIPTION
Requires running KITOS and PubSub databases, trusted HTTPS certificates, and a
KITOS global-admin API token supplied through PublishToken or USER_SYNC_PUBLISH_TOKEN.
AdminCredential supplies a KITOS account with local-admin permission in the target
organization. If omitted, the script prompts for it. Internal endpoints require
cookie authentication, not an API token.
StartPubSub starts a temporary local PubSub process with user sync enabled and
stops that process on exit. Its KITOS account needs API access and IsPubSubUser.
Without StartPubSub, configure the running PubSub instance with UserSync enabled.
ConnectUsers explicitly enables the organization's FK Organisation users connection.
Apply explicitly removes organization access, subject to confirmation. Without
Apply, the event remains pending. Use only disposable test organizations/users.
.EXAMPLE
.\DeploymentScripts\TestExternalUserDeletion.ps1 -OrganizationUuid <guid> -ExternalUserUuid <external-sso-guid>
.EXAMPLE
.\DeploymentScripts\TestExternalUserDeletion.ps1 -OrganizationUuid <guid> -ExternalUserUuid <external-sso-guid> -StartPubSub -Duplicate
.EXAMPLE
.\DeploymentScripts\TestExternalUserDeletion.ps1 -OrganizationUuid <guid> -ExternalUserUuid <external-sso-guid> -Apply
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)][guid]$OrganizationUuid,
    [Parameter(Mandatory)][guid]$ExternalUserUuid,
    [string]$PublishToken = $env:USER_SYNC_PUBLISH_TOKEN,
    [PSCredential]$AdminCredential,
    [uri]$KitosBaseUrl = 'https://localhost:44300',
    [uri]$PubSubBaseUrl = 'https://localhost:7226',
    [ValidateNotNullOrEmpty()][string]$MessageId = [guid]::NewGuid().ToString(),
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 120,
    [switch]$StartPubSub,
    [PSCredential]$PubSubCredential,
    [switch]$ConnectUsers,
    [switch]$Duplicate,
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
foreach ($url in @($KitosBaseUrl, $PubSubBaseUrl)) {
    if (!$url.IsAbsoluteUri -or $url.Scheme -ne 'https' -or $url.UserInfo -or
        $url.AbsolutePath -ne '/' -or $url.Query -or $url.Fragment) {
        throw 'Supply HTTPS base URLs without credentials, paths, queries, or fragments.'
    }
}
if ($OrganizationUuid -eq [guid]::Empty -or $ExternalUserUuid -eq [guid]::Empty) {
    throw 'OrganizationUuid and ExternalUserUuid must be non-empty UUIDs.'
}
if ([string]::IsNullOrWhiteSpace($MessageId) -or $MessageId.Length -gt 200) {
    throw 'MessageId must contain between 1 and 200 characters.'
}
if ($StartPubSub -and (!$KitosBaseUrl.IsLoopback -or !$PubSubBaseUrl.IsLoopback)) {
    throw '-StartPubSub is restricted to local KITOS and PubSub URLs.'
}
if ([string]::IsNullOrWhiteSpace($PublishToken)) {
    throw 'Supply -PublishToken or set USER_SYNC_PUBLISH_TOKEN to a KITOS global-admin API token.'
}

$kitos = $KitosBaseUrl.AbsoluteUri.TrimEnd('/')
$adminSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$adminHeaders = @{}
$connectionUrl = "$kitos/api/v2/internal/organizations/$OrganizationUuid/sts-organization-synchronization/users"
$changesUrl = "$kitos/api/v2/internal/organization/$OrganizationUuid/users/external-changes"
$messageSearch = [uri]::EscapeDataString($MessageId)
$process = $null
$previousEnvironment = @{}

try {
    if ($WhatIfPreference) {
        Write-Host 'Would check the users connection, optionally start PubSub, publish a deletion, and wait for ingestion.'
        if ($ConnectUsers) { Write-Host 'Would enable the FK Organisation users connection if disconnected.' }
        if ($Apply) { Write-Host 'Would apply the deletion after ingestion.' }
        return
    }

    if (!$AdminCredential) {
        $AdminCredential = Get-Credential -Message 'KITOS admin login (local-admin permission in the target organization)'
    }
    if (!$AdminCredential) { throw 'A KITOS admin credential is required for internal endpoints.' }
    $csrfToken = Invoke-RestMethod -Uri "$kitos/api/authorize/antiforgery" -WebSession $adminSession -TimeoutSec 30
    if ([string]::IsNullOrWhiteSpace($csrfToken)) { throw 'KITOS did not return a CSRF token.' }
    $adminHeaders['X-XSRF-TOKEN'] = $csrfToken
    $loginBody = @{
        Email = $AdminCredential.UserName
        Password = $AdminCredential.GetNetworkCredential().Password
        RememberMe = $false
    } | ConvertTo-Json -Compress
    try {
        Invoke-RestMethod -Method Post -Uri "$kitos/api/authorize" -WebSession $adminSession `
            -Headers $adminHeaders -ContentType 'application/json' -Body $loginBody -TimeoutSec 30 | Out-Null
    }
    finally { $loginBody = $null }

    $connection = Invoke-RestMethod -Uri "$connectionUrl/connection-status" -WebSession $adminSession -Headers $adminHeaders -TimeoutSec 30
    if (!$connection.connected) {
        if (!$ConnectUsers) {
            throw 'FK Organisation users are disconnected. Connect them first or use -ConnectUsers.'
        }
        if (!$PSCmdlet.ShouldProcess($OrganizationUuid, 'Enable FK Organisation users connection')) { return }
        Invoke-RestMethod -Method Post -Uri "$connectionUrl/connection" -WebSession $adminSession -Headers $adminHeaders -TimeoutSec 30 | Out-Null
    }

    if ($StartPubSub) {
        $listener = [System.Net.Sockets.TcpClient]::new()
        try {
            $listener.Connect($PubSubBaseUrl.Host, $PubSubBaseUrl.Port)
            throw 'The PubSub port is already in use. Omit -StartPubSub to use the running instance.'
        }
        catch [System.Net.Sockets.SocketException] {
            # No listener: this script can own the temporary PubSub process.
        }
        finally { $listener.Dispose() }

        if (!$PubSubCredential) {
            $PubSubCredential = Get-Credential -Message 'KITOS delivery account (API access and IsPubSubUser required)'
        }
        if (!$PubSubCredential) { throw 'A KITOS PubSub delivery credential is required.' }
        $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
        $project = Join-Path $PSScriptRoot '..\PubSub.Application.Api\PubSub.Application.Api.csproj'
        $environment = @{
            ASPNETCORE_ENVIRONMENT = 'Local'
            ASPNETCORE_URLS = $PubSubBaseUrl.AbsoluteUri.TrimEnd('/')
            UserSync__Enabled = 'true'
            UserSync__EnableStub = 'true'
            UserSync__KitosEndpoint = "$kitos/api/v2/integrations/fk-organisation/user-changes"
            UserSync__KitosEmail = $PubSubCredential.UserName
            UserSync__KitosPassword = $PubSubCredential.GetNetworkCredential().Password
            JwtValidation__ApiUrl = $kitos
        }
        try {
            foreach ($name in $environment.Keys) {
                $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
                [Environment]::SetEnvironmentVariable($name, $environment[$name], 'Process')
            }
            $process = Start-Process -FilePath $dotnet -ArgumentList @('run', '--project', "`"$project`"", '--no-launch-profile') -PassThru -NoNewWindow
        }
        finally {
            foreach ($name in $previousEnvironment.Keys) {
                if ($null -eq $previousEnvironment[$name]) {
                    Remove-Item "Env:\$name" -ErrorAction Stop
                }
                else {
                    [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
                }
            }
            $environment.Clear()
        }

        $startupDeadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        while ($true) {
            if ($process.HasExited) { throw "PubSub exited during startup with code $($process.ExitCode)." }
            try {
                Invoke-RestMethod -Uri "$($PubSubBaseUrl.AbsoluteUri.TrimEnd('/'))/swagger/v1/swagger.json" -TimeoutSec 5 | Out-Null
                break
            }
            catch {
                if ([DateTime]::UtcNow -ge $startupDeadline) { throw "PubSub did not become ready: $($_.Exception.Message)" }
                Start-Sleep -Seconds 2
            }
        }
    }

    $delivery = & "$PSScriptRoot\TestUserSync.ps1" -PubSubBaseUrl $PubSubBaseUrl `
        -OrganizationUuid $OrganizationUuid -ExternalUserUuid $ExternalUserUuid `
        -MessageId $MessageId -Duplicate:$Duplicate -PublishToken $PublishToken
    Write-Host "Queued delivery $($delivery.uuid); waiting for KITOS message $MessageId."

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $change = $null
    while (!$change) {
        $skip = 0
        do {
            $page = Invoke-RestMethod -Uri "${changesUrl}?search=$messageSearch&skip=$skip&take=100" -WebSession $adminSession -Headers $adminHeaders -TimeoutSec 30
            $change = $page.items | Where-Object { $_.externalMessageId -eq $MessageId } | Select-Object -First 1
            $skip += 100
            if ([DateTime]::UtcNow -ge $deadline -and !$change) {
                throw "Timed out waiting for KITOS ingestion. Check PubSub delivery logs for delivery $($delivery.uuid) (409: disconnected/conflicting event; 401/403: authentication/permissions)."
            }
        } while (!$change -and $skip -lt $page.total)
        if (!$change) { Start-Sleep -Seconds 2 }
    }
    if ([guid]$change.externalUserUuid -ne $ExternalUserUuid) {
        throw 'The stored message belongs to a different external user.'
    }

    if ($Apply -and $PSCmdlet.ShouldProcess("$ExternalUserUuid in organization $OrganizationUuid", 'Apply external deletion and remove organization access')) {
        $change = Invoke-RestMethod -Method Post -Uri "$changesUrl/$($change.uuid)/apply" -WebSession $adminSession -Headers $adminHeaders -TimeoutSec 30
    }
    $change
}
finally {
    if ($process -and !$process.HasExited) {
        # dotnet run owns an application child process; stop only our process tree.
        & taskkill.exe /PID $process.Id /T /F | Out-Null
        if ($LASTEXITCODE -ne 0 -and !$process.HasExited) {
            throw "Could not stop the temporary PubSub process tree (PID $($process.Id))."
        }
        $process.WaitForExit()
    }
}
