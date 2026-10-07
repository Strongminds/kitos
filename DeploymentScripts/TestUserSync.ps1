param(
    [Parameter(Mandatory)][uri]$PubSubBaseUrl,
    [Parameter(Mandatory)][guid]$OrganizationUuid,
    [Parameter(Mandatory)][guid]$ExternalUserUuid,
    [string]$PublishToken = $env:USER_SYNC_PUBLISH_TOKEN,
    [string]$MessageId = [guid]::NewGuid().ToString(),
    [switch]$Duplicate
)
$ErrorActionPreference = 'Stop'
if ($PubSubBaseUrl.Scheme -ne 'https') { throw 'Use an HTTPS PubSub URL.' }
if ([string]::IsNullOrWhiteSpace($PublishToken)) {
    throw 'Supply -PublishToken or set USER_SYNC_PUBLISH_TOKEN to a test publisher token.'
}
$payload = @{
    externalMessageId = $MessageId
    organizationUuid = $OrganizationUuid.ToString()
    externalUserUuid = $ExternalUserUuid.ToString()
    changeType = 1
    occurredAt = $null
} | ConvertTo-Json -Compress
$headers = @{ Authorization = 'Bearer ' + $PublishToken }
$uri = $PubSubBaseUrl.AbsoluteUri.TrimEnd('/') + '/user-sync/stub'
$first = Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType 'application/json' -Body $payload
if ($Duplicate) {
    $second = Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType 'application/json' -Body $payload
    if ($first.uuid -ne $second.uuid) { throw 'Duplicate submission returned a different delivery ID.' }
}
$first
