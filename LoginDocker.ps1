#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $true)]
    [string]$DockerUsername,

    [Parameter(Mandatory = $true)]
    [string]$DockerPassword,

    [Parameter(Mandatory = $false)]
    [string]$DockerRegistry = "docker.io"
)

Write-Host "Logging in to Docker registry: $DockerRegistry"
docker login --username $DockerUsername --password $DockerPassword $DockerRegistry
