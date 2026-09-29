param(
    [Parameter(Mandatory)][uri]$PubSubBaseUrl,
    [Parameter(Mandatory)][guid]$OrganizationUuid,
    [Parameter(Mandatory)][guid]$ExternalUserUuid,
    [string]$MessageId = [guid]::NewGuid().ToString(),
    [switch]$Duplicate
)
$ErrorActionPreference = 'Stop'
if ($PubSubBaseUrl.Scheme -ne 'https') { throw 'Use an HTTPS PubSub URL.' }
if ([string]::IsNullOrWhiteSpace($env:USER_SYNC_PUBLISH_TOKEN)) {
    throw 'Set USER_SYNC_PUBLISH_TOKEN to a test publisher token. Do not pass credentials on the command line.'
}
$payload = @{
    externalMessageId = $MessageId
    organizationUuid = $OrganizationUuid.ToString()
    externalUserUuid = $ExternalUserUuid.ToString()
    changeType = 1
    occurredAt = $null
} | ConvertTo-Json -Compress
$headers = @{ Authorization = 'Bearer ' + $env:USER_SYNC_PUBLISH_TOKEN }
$uri = $PubSubBaseUrl.AbsoluteUri.TrimEnd('/') + '/user-sync/stub'
$first = Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType 'application/json' -Body $payload
if ($Duplicate) {
    $second = Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType 'application/json' -Body $payload
    if ($first.uuid -ne $second.uuid) { throw 'Duplicate submission returned a different delivery ID.' }
}
$first
