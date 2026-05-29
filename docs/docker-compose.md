# Running KITOS with Docker Compose

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) (v20.10+)
- [Docker Compose](https://docs.docker.com/compose/install/) (v2.0+)

## Quick Start

From the repository root:

```bash
docker compose up
```

This starts the full KITOS stack. First run will build images and apply database migrations automatically.

## Services

| Service | Port | Description |
|---------|------|-------------|
| **kitos-api** | [localhost:5000](http://localhost:5000) | Main KITOS API + legacy AngularJS UI |
| **pubsub-api** | [localhost:5100](http://localhost:5100) | PubSub API (event messaging) |
| **postgres** | localhost:5432 | PostgreSQL 17 (3 databases) |
| **rabbitmq** | [localhost:15672](http://localhost:15672) | RabbitMQ management UI |

## Databases

PostgreSQL is initialized with three databases:

| Database | Purpose |
|----------|---------|
| `kitos` | Main application database |
| `kitos_hangfire` | Background job storage (Hangfire) |
| `kitos_pubsub` | PubSub event messaging database |

**Credentials:** `kitos` / `kitos` (user/password)

## Common Commands

```bash
# Start all services (foreground)
docker compose up

# Start in background
docker compose up -d

# Rebuild images after code changes
docker compose up --build

# Stop all services
docker compose down

# Stop and remove volumes (reset databases)
docker compose down -v

# View logs for a specific service
docker compose logs -f kitos-api

# Run only migrations (useful for debugging)
docker compose run --rm migrate-db
docker compose run --rm migrate-pubsub-db
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

## Troubleshooting

### Port conflicts

If ports 5000, 5100, 5432, 5672, or 15672 are in use, stop conflicting services or modify the port mappings in `docker-compose.yml`.

### Database issues

Reset the database state:

```bash
docker compose down -v
docker compose up
```

### Build failures

Ensure you're building from the repo root (where `docker-compose.yml` lives):

```bash
docker compose build --no-cache
```

## Notes

- The legacy AngularJS frontend is served directly by `kitos-api` (ASP.NET)
- The [new frontend](https://github.com/os2kitos/kitos_frontend) is a separate repository and not included in this compose setup
- RabbitMQ management UI uses default credentials: `guest` / `guest`
- Mail is configured to use a pickup directory (`/tmp/maildrop/`) — no SMTP server needed for local dev
