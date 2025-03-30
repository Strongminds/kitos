param (
    [string]$targetEnvironment = "staging"
)

Write-Host "Deploying PubSub to environment: $targetEnvironment"

# Configure
$keyPath = "C:\TeamCity\buildAgent\.ssh\id_rsa"
$composeFile = Join-Path $PSScriptRoot "..\PubSub.Application\docker-compose.yml"
if (-Not (Test-Path $composeFile)) {
    Write-Error "Compose file not found at: $composeFile"
    exit 1
}
$remoteUser = "kitosadmintest"
$remoteHost = "10.212.74.11"
$remotePath = "/home/kitosadmintest/app"
$remoteTarget = "${remoteUser}@${remoteHost}:${remotePath}"

# Copy compose-file via SCP
Write-Host "Copying $composeFile to $remoteTarget"
scp -i $keyPath -o ConnectTimeout=10 $composeFile $remoteTarget


if ($LASTEXITCODE -ne 0) {
    Write-Error "SCP failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Run docker compose via SSH
Write-Host "Running docker-compose on remote host..."
$sshCommand = "cd $remotePath && docker-compose pull && docker-compose up -d"
ssh -i $keyPath "${remoteUser}@${remoteHost}" $sshCommand

if ($LASTEXITCODE -ne 0) {
    Write-Error "SSH command failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Pubsub deployment completed successfully."
