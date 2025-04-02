.$PSScriptRoot\SetupPubsubEnviroment.ps1

param (
    [Parameter(Mandatory = $true)]
    [string]$targetEnvironment,

    [Parameter(Mandatory = $true)]
    [string]$ASPNETCORE_ENVIRONMENT
)

# Load environment secrets from AWS for the specified target environment
Write-Host "Loading environment secrets for $targetEnvironment..."
Load-Environment-Secrets-From-Aws -envName $targetEnvironment

Write-Host "Deploying PubSub to environment: $targetEnvironment"

# Configure local paths and remote details
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

# Copy the docker-compose file to the remote host
Write-Host "Copying $composeFile to $remoteTarget"
Get-Content -Path $composeFile -Raw | ssh -i $keyPath `
    -o Compression=no -o IPQoS=throughput -o StrictHostKeyChecking=accept-new `
    "$remoteUser@$remoteHost" "cat > $remotePath/docker-compose.yml"

if ($LASTEXITCODE -ne 0) {
    Write-Error "SCP of docker-compose.yml failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Generate the .env file content with the passed variables and AWS loaded secrets
$envContent = @"
ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT
RABBIT_MQ_USER=$RABBIT_MQ_USER
RABBIT_MQ_PASSWORD=$RABBIT_MQ_PASSWORD
PUBSUB_API_KEY=$PUBSUB_API_KEY
IDP_HOST_MAPPING=$IDP_HOST_MAPPING
CERT_PASSWORD=$CERT_PASSWORD
"@

# Copy the .env file to the remote host
Write-Host "Copying .env file to remote host..."
$envContent | ssh -i $keyPath `
    -o Compression=no -o IPQoS=throughput -o StrictHostKeyChecking=accept-new `
    "$remoteUser@$remoteHost" "cat > $remotePath/.env"
if ($LASTEXITCODE -ne 0) {
    Write-Error "SCP of .env file failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Cleanup previous containers using docker-compose down and deploy new ones
Write-Host "Cleaning up old containers and deploying with docker-compose..."
$sshCommand = @"
cd $remotePath;
docker-compose down || true;
docker-compose pull;
docker-compose up -d --remove-orphans;
docker image prune -f;
"@

if ($LASTEXITCODE -ne 0) {
    Write-Error "SSH command failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "PubSub deployment completed successfully."
