# KITOS Containerization Guide

This document describes how to run KITOS in containers and how configuration, secrets, and certificates are managed.

## Running Locally with Docker Compose

```bash
# 1. Copy the environment template and fill in certificate passwords
cp .env.example .env

# 2. Place certificate PFX files in ./certs/
#    - certs/kitos-local-pfx                (SSO Service Provider)
#    - certs/ADG_EXTTEST_Adgangsstyring_2   (STS Adgangsstyring)
#    - certs/ORG_EXTTEST_Organisation_2     (STS Organisation)

# 3. Start the stack
docker compose up
```

The API will be available at `http://localhost:5000`.

### Services

| Service | Port | Description |
|---------|------|-------------|
| kitos-api | 5000 | Main KITOS API + legacy AngularJS UI |
| pubsub-api | 5100 | PubSub event service |
| postgres | 5432 | PostgreSQL database |
| rabbitmq | 5672 / 15672 | Message broker (management UI on 15672) |

### What `.env` Needs to Contain

See `.env.example` for the full template. At minimum:

```env
SSO_CERT_FILE_NAME=kitos-local-pfx
STS_CERT_FILE_NAME=ADG_EXTTEST_Adgangsstyring_2
STS_ORG_CERT_FILE_NAME=ORG_EXTTEST_Organisation_2
SSO_CERT_PASSWORD=<password for kitos-local-pfx>
STS_CERT_PASSWORD=<password for ADG_EXTTEST_Adgangsstyring_2>
STS_ORG_CERT_PASSWORD=<password for ORG_EXTTEST_Organisation_2>
SSO_CERT_THUMBPRINT=<thumbprint for SSO certificate>
STS_CERT_THUMBPRINT=<thumbprint for STS certificate>
STS_ORG_CERT_THUMBPRINT=<thumbprint for STS organisation certificate>
```

## Configuration

KITOS uses ASP.NET Core's layered configuration system:

1. **`appsettings.json`** — base configuration baked into the image
2. **Environment variables** — override any setting using `__` as section separator  
   Example: `AppSettings__BaseUrl` overrides `AppSettings.BaseUrl`
3. **`appsettings.{ENVIRONMENT}.json`** — per-environment overrides (optional)

### Kubernetes Deployment

In Kubernetes, configuration is provided via:

- **ConfigMap** → mounted as environment variables or an `appsettings.Production.json` file
- **Secrets** → mounted as environment variables for sensitive values (passwords, connection strings, certificate passwords)

Example ConfigMap entries:
```yaml
AppSettings__BaseUrl: "https://kitos.example.dk"
AppSettings__SsoServiceProviderId: "https://kitos.example.dk"
Database__Provider: "PostgreSql"
```

Example Secret entries:
```yaml
ConnectionStrings__KitosContext: "Host=...;Password=..."
AppSettings__SsoCertPassword: "..."
AppSettings__StsCertPassword: "..."
AppSettings__StsOrganisationCertPassword: "..."
```

## Certificates

KITOS uses X.509 certificates for communication with Danish government services (KOMBIT STS, Serviceplatformen).

### How It Works

The `CertificateLoader` class supports two modes:

1. **PFX file-based** (containers/Linux) — set `*CertFilePath` config keys to mount PFX files
2. **Windows Certificate Store** (legacy/IIS) — leave `*CertFilePath` empty to use thumbprint-based store lookup

The system automatically falls back: if a `*CertFilePath` is set, the PFX file is loaded and validated against the configured thumbprint. If not, the Windows certificate store is used with the configured thumbprint.

### Certificate Config Keys

| Purpose | File Path Key | Password Key | Thumbprint Key (fallback) |
|---------|--------------|--------------|--------------------------|
| SSO Service Provider | `AppSettings:SsoCertFilePath` | `AppSettings:SsoCertPassword` | `AppSettings:SsoCertificateThumbprint` |
| STS (Adgangsstyring) | `AppSettings:StsCertFilePath` | `AppSettings:StsCertPassword` | `AppSettings:StsCertificateThumbprint` |
| STS Organisation | `AppSettings:StsOrganisationCertFilePath` | `AppSettings:StsOrganisationCertPassword` | `AppSettings:StsOrganisationCertificateThumbprint` |

### Docker Compose Cert Setup

Place PFX files in the `./certs/` directory at the repo root. They are mounted read-only into the container at `/etc/ssl/certs/`:

```yaml
volumes:
  - ./certs/kitos-local-pfx:/etc/ssl/certs/kitos-local-pfx:ro
  - ./certs/ADG_EXTTEST_Adgangsstyring_2:/etc/ssl/certs/ADG_EXTTEST_Adgangsstyring_2:ro
  - ./certs/ORG_EXTTEST_Organisation_2:/etc/ssl/certs/ORG_EXTTEST_Organisation_2:ro
```

### Kubernetes Cert Setup

Mount PFX files from Kubernetes Secrets as volumes:

```yaml
volumes:
  - name: certs
    secret:
      secretName: kitos-certs
volumeMounts:
  - name: certs
    mountPath: /etc/ssl/certs
    readOnly: true
```

Set the file path environment variables to point to the mounted files:
```yaml
AppSettings__SsoCertFilePath: /etc/ssl/certs/kitos-local-pfx
AppSettings__StsCertFilePath: /etc/ssl/certs/ADG_EXTTEST_Adgangsstyring_2
AppSettings__StsOrganisationCertFilePath: /etc/ssl/certs/ORG_EXTTEST_Organisation_2
AppSettings__SsoCertificateThumbprint: "<thumbprint>"
AppSettings__StsCertificateThumbprint: "<thumbprint>"
AppSettings__StsOrganisationCertificateThumbprint: "<thumbprint>"
```

## Database Migrations

Migrations run as a separate init step before the API starts (the `migrate-db` service in Docker Compose, or an init-container in Kubernetes). The API does NOT auto-migrate on startup.

```bash
# Run migrations manually against a running database
docker compose run --rm migrate-db
```

## Kubernetes Deployment

Full Kubernetes manifests are available in the `k8s/` directory. See [`k8s/README.md`](k8s/README.md) for detailed instructions.

### Quick Start

```bash
# Apply all manifests
kubectl apply -f k8s/ --recursive

# Or apply in order (recommended for first deployment)
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/rabbitmq/
kubectl apply -f k8s/kitos-api/
kubectl apply -f k8s/pubsub-api/
kubectl apply -f k8s/ingress.yaml
```

### Architecture Overview

The Kubernetes deployment includes:

| Component | Image | Replicas | Notes |
|-----------|-------|----------|-------|
| postgres | postgres:17 | 1 | 10Gi PVC, init script creates 3 databases |
| rabbitmq | rabbitmq:3-management | 1 | AMQP + management UI |
| kitos-api | kitos-api:latest | 2 | Main API, serves legacy UI |
| pubsub-api | pubsub-api:latest | 1 | Event subscription service |

### Secrets Required

Before deployment, create these secrets manually (they contain sensitive values not stored in git):

- `postgres-credentials` — database password
- `rabbitmq-credentials` — broker password
- `kitos-api-secrets` — connection strings, JWT key, cert passwords
- `kitos-sso-certificates` — PFX certificate files
- `pubsub-api-secrets` — connection string, RabbitMQ password, API key
- `kitos-tls` — TLS certificate for ingress

### Migration Pattern

Database schema migrations run as a Kubernetes Job before deployment:

```bash
kubectl apply -f k8s/kitos-api/migration-job.yaml
kubectl wait --for=condition=complete job/kitos-api-migration -n kitos --timeout=120s
# Then roll out new deployment
kubectl apply -f k8s/kitos-api/deployment.yaml
```
