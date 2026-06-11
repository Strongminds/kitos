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

| Service    | Port         | Description                             |
| ---------- | ------------ | --------------------------------------- |
| kitos-api  | 5000         | Main KITOS API + legacy AngularJS UI    |
| pubsub-api | 5100         | PubSub event service                    |
| postgres   | 5432         | PostgreSQL database                     |
| rabbitmq   | 5672 / 15672 | Message broker (management UI on 15672) |

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

## Certificates

KITOS uses X.509 certificates for communication with Danish government services (KOMBIT STS, Serviceplatformen).

### How It Works

The `CertificateLoader` class supports two modes:

1. **PFX file-based** (containers/Linux) — set `*CertFilePath` config keys to mount PFX files
2. **Windows Certificate Store** (legacy/IIS) — leave `*CertFilePath` empty to use thumbprint-based store lookup

The system automatically falls back: if a `*CertFilePath` is set, the PFX file is loaded and validated against the configured thumbprint. If not, the Windows certificate store is used with the configured thumbprint.

### Certificate Config Keys

| Purpose              | File Path Key                             | Password Key                              | Thumbprint Key (fallback)                          |
| -------------------- | ----------------------------------------- | ----------------------------------------- | -------------------------------------------------- |
| SSO Service Provider | `AppSettings:SsoCertFilePath`             | `AppSettings:SsoCertPassword`             | `AppSettings:SsoCertificateThumbprint`             |
| STS (Adgangsstyring) | `AppSettings:StsCertFilePath`             | `AppSettings:StsCertPassword`             | `AppSettings:StsCertificateThumbprint`             |
| STS Organisation     | `AppSettings:StsOrganisationCertFilePath` | `AppSettings:StsOrganisationCertPassword` | `AppSettings:StsOrganisationCertificateThumbprint` |

### STS Certificate Validation in Containers

To replicate production behavior locally, keep strict STS certificate checks enabled:

- `AppSettings__StsCertificateValidationMode=ChainTrust`
- `AppSettings__StsCertificateRevocationMode=Online`

In Docker Compose these are mapped from:

- `STS_CERT_VALIDATION_MODE` (default `ChainTrust`)
- `STS_CERT_REVOCATION_MODE` (default `Online`)

Only for non-production troubleshooting, you can temporarily relax checks to values like:

- `AppSettings__StsCertificateValidationMode` (for example `None`)
- `AppSettings__StsCertificateRevocationMode` (for example `NoCheck`)

### Docker Compose Cert Setup

Place PFX files in the `./certs/` directory at the repo root. They are mounted read-only into the container at `/etc/ssl/certs/`:

```yaml
volumes:
  - ./certs/kitos-local-pfx:/etc/ssl/certs/kitos-local-pfx:ro
  - ./certs/ADG_EXTTEST_Adgangsstyring_2:/etc/ssl/certs/ADG_EXTTEST_Adgangsstyring_2:ro
  - ./certs/ORG_EXTTEST_Organisation_2:/etc/ssl/certs/ORG_EXTTEST_Organisation_2:ro
```

To replicate production trust behavior in Docker, also place trust-chain certificates
(`.crt` in PEM format) in `./certs/` (for example root/intermediate CA). The `kitos-api`
container imports `./certs/*.crt` into `/usr/local/share/ca-certificates/` and runs
`update-ca-certificates` on startup before launching the API.

## Database Migrations

Migrations run as a separate init step before the API starts (the `migrate-db` service in Docker Compose, or an init-container in Kubernetes). The API does NOT auto-migrate on startup.

```bash
# Run migrations manually against a running database
docker compose run --rm migrate-db
```
