# Running KITOS with Podman Compose

## Prerequisites

- [Podman](https://podman.io/docs/installation) (v4.0+)
- [Podman Compose](https://github.com/containers/podman-compose) (v1.0+)

## Quick Start

From the repository root:

```bash
podman compose up
```

This starts the full KITOS stack. First run will build images and apply database migrations automatically.

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
| `kitos_hangfire` | Background job storage (Hangfire) |
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

# Run only migrations (useful for debugging)
podman compose run --rm migrate-db
podman compose run --rm migrate-pubsub-db
```

## How Migrations Work

Database migrations run automatically before the APIs start:

1. `migrate-db` service applies EF Core migrations to the `kitos` database
2. `migrate-pubsub-db` service applies migrations to `kitos_pubsub`
3. APIs only start after their respective migration service exits successfully

This mirrors the Kubernetes init-container pattern.

## Environment Variables

Key environment variables can be overridden in a `.env` file at the repo root:

```env
# Example overrides
POSTGRES_PASSWORD=custom_password
APP_USER=custom_user
APP_PASSWORD=custom_password
```
