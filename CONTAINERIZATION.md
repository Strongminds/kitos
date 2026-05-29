# KITOS Containerization Guide

This document describes how to run KITOS in containers and how configuration, secrets, and certificates are managed.

## Running Locally with Docker Compose

```bash
# 1. Copy the environment template and fill in certificate passwords
cp .env.example .env

# 2. Place certificate PFX files in ./certs/
#    - certs/sso-sp.pfx    (SSO Service Provider)
#    - certs/sts.pfx       (STS Adgangsstyring)
#    - certs/sts-org.pfx   (STS Organisation)

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
SSO_CERT_PASSWORD=<password for sso-sp.pfx>
STS_CERT_PASSWORD=<password for sts.pfx>
STS_ORG_CERT_PASSWORD=<password for sts-org.pfx>
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

The system automatically falls back: if a `*CertFilePath` is set, the PFX file is loaded. If not, the Windows certificate store is used with the configured thumbprint.

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
  - ./certs/sso-sp.pfx:/etc/ssl/certs/sso-sp.pfx:ro
  - ./certs/sts.pfx:/etc/ssl/certs/sts.pfx:ro
  - ./certs/sts-org.pfx:/etc/ssl/certs/sts-org.pfx:ro
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
AppSettings__SsoCertFilePath: /etc/ssl/certs/sso-sp.pfx
AppSettings__StsCertFilePath: /etc/ssl/certs/sts.pfx
AppSettings__StsOrganisationCertFilePath: /etc/ssl/certs/sts-org.pfx
```

## Database Migrations

Migrations run as a separate init step before the API starts (the `migrate-db` service in Docker Compose, or an init-container in Kubernetes). The API does NOT auto-migrate on startup.

```bash
# Run migrations manually against a running database
docker compose run --rm migrate-db
```
