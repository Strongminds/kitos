param(
    [Parameter(Mandatory=$true)][string]$targetEnvironment
    )

# Stop on first error
$ErrorActionPreference = "Stop"

$remoteUser = "kitosadmintest"
$remoteHost = "10.212.74.11"
$composeFile = "../PubSub.Application/docker-compose.yml"

# Copy docker-compose file
scp -i id_rsa $composeFile "$remoteUser@$remoteHost:/home/kitosadmintest/app"

# Run remote docker compose
ssh -i id_rsa $remoteUser@$remoteHost "cd /home/kitosadmintest/app && docker-compose pull && docker-compose up -d"
