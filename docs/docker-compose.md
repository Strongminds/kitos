# Running KITOS with Podman Compose

## Prerequisites

- [Podman](https://podman.io/docs/installation) (v4.0+)
- [Podman Compose](https://github.com/containers/podman-compose) (v1.0+)

## Quick Start

From the repository root:

```bash
podman compose up
```

This starts the local KITOS stack. Databases are still left in base state until `PrepareLocalDatabase.ps1` is run.

## Services

| Service        | Port                                      | Description                          |
| -------------- | ----------------------------------------- | ------------------------------------ |
| **kitos-api**  | [localhost:5000](http://localhost:5000)   | Main KITOS API + legacy AngularJS UI |
| **pubsub-api** | [localhost:5100](http://localhost:5100)   | PubSub API (event messaging)         |
| **postgres**   | localhost:5432                            | PostgreSQL 17 (3 databases)          |
| **rabbitmq**   | [localhost:15672](http://localhost:15672) | RabbitMQ management UI               |

## Databases

PostgreSQL is initialized with three databases:

| Database         | Purpose                           |
| ---------------- | --------------------------------- |
| `kitos`          | Main application database         |
| `kitos_hangfiredb` | Background job storage (Hangfire) |
| `kitos_pubsub`   | PubSub event messaging database   |

**Credentials:** `kitos` / `kitos` (user/password)

## Common Commands

```bash
# Start all services (foreground)
podman compose up

# Start in background
podman compose up -d

# Rebuild images after code changes
podman compose up --build

# Stop all services
podman compose down

# Stop and remove volumes (reset databases)
podman compose down -v

# View logs for a specific service
podman compose logs -f kitos-api
```

## Prepare Local Databases

Run database preparation manually per developer after the containers are running:

```powershell
.\DeploymentScripts\PrepareLocalDatabase.ps1 `
  -kitosDbConnectionString "Host=localhost;Port=5432;Database=kitos;Username=kitos;Password=kitos" `
  -hangfireDbConnectionString "Host=localhost;Port=5432;Database=kitos_hangfiredb;Username=kitos;Password=kitos"
```

This script is responsible for preparing the local KITOS/Hangfire databases for development use.

To prepare the PubSub database for the containerized stack:

```powershell
.\DeploymentScripts\PrepareLocalPubSubDatabase.Postgres.ps1
```

## Environment Variables

Key environment variables can be overridden in a `.env` file at the repo root:

```env
# Example overrides
POSTGRES_PASSWORD=custom_password
APP_USER=custom_user
APP_PASSWORD=custom_password
```
