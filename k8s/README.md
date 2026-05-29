# KITOS Kubernetes Deployment

This directory contains Kubernetes manifests for deploying the full KITOS stack.

## Prerequisites

- Kubernetes cluster (1.25+)
- `kubectl` configured with cluster access
- nginx ingress controller installed
- Storage class available for PersistentVolumeClaims

## Applying Manifests

Apply all manifests recursively:

```bash
kubectl apply -f k8s/ --recursive
```

Or apply in dependency order:

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/rabbitmq/
kubectl apply -f k8s/kitos-api/configmap.yaml
kubectl apply -f k8s/kitos-api/secret.yaml
kubectl apply -f k8s/kitos-api/migration-job.yaml
# Wait for migration to complete
kubectl wait --for=condition=complete job/kitos-api-migration -n kitos --timeout=120s
kubectl apply -f k8s/kitos-api/deployment.yaml
kubectl apply -f k8s/kitos-api/service.yaml
kubectl apply -f k8s/pubsub-api/
kubectl apply -f k8s/ingress.yaml
```

## Secrets That Must Be Created Manually

The following secrets contain placeholder values and **must** be replaced before deployment:

### 1. `postgres-credentials`

```bash
kubectl create secret generic postgres-credentials \
  --namespace kitos \
  --from-literal=password='<POSTGRES_PASSWORD>'
```

### 2. `rabbitmq-credentials`

```bash
kubectl create secret generic rabbitmq-credentials \
  --namespace kitos \
  --from-literal=password='<RABBITMQ_PASSWORD>'
```

### 3. `kitos-api-secrets`

Update `k8s/kitos-api/secret.yaml` with base64-encoded values for:
- `ConnectionStrings__KitosContext` — PostgreSQL connection string for the main database
- `ConnectionStrings__HangfireConnection` — PostgreSQL connection string for Hangfire
- `AppSettings__SecurityKeyString` — JWT signing key
- `AppSettings__SsoCertPassword` — Password for SSO SP certificate PFX
- `AppSettings__StsCertPassword` — Password for STS certificate PFX
- `AppSettings__StsOrganisationCertPassword` — Password for STS Organisation certificate PFX

### 4. `kitos-sso-certificates`

Certificate PFX files must be loaded into a Kubernetes secret:

```bash
kubectl create secret generic kitos-sso-certificates \
  --namespace kitos \
  --from-file=sso-sp.pfx=./certs/sso-sp.pfx \
  --from-file=sts.pfx=./certs/sts.pfx \
  --from-file=sts-org.pfx=./certs/sts-org.pfx
```

### 5. `pubsub-api-secrets`

Update `k8s/pubsub-api/secret.yaml` with base64-encoded values for:
- `ConnectionStrings__PubSubContext` — PostgreSQL connection string for PubSub database
- `RabbitMQ__Password` — RabbitMQ password
- `ApiKey` — Internal API key for service-to-service auth

### 6. `kitos-tls`

TLS certificate for the ingress:

```bash
kubectl create secret tls kitos-tls \
  --namespace kitos \
  --cert=./tls/tls.crt \
  --key=./tls/tls.key
```

## Migration Job Pattern

Database migrations are run as a Kubernetes Job (`migration-job.yaml`) before deploying a new version:

1. Update the image tag in `migration-job.yaml` to the new version
2. Apply the job: `kubectl apply -f k8s/kitos-api/migration-job.yaml`
3. Wait for completion: `kubectl wait --for=condition=complete job/kitos-api-migration -n kitos`
4. Once migrations succeed, update the image tag in `deployment.yaml` and apply

For CI/CD pipelines, the migration job should be applied and awaited before the deployment rollout begins. If the migration fails, the deployment should not proceed.

## Customization

- **Host**: Replace `kitos.example.dk` in `ingress.yaml` and `kitos-api/configmap.yaml`
- **Images**: Replace `kitos-api:latest` and `pubsub-api:latest` with your registry/tag
- **Resources**: Adjust resource requests/limits based on actual usage
- **Replicas**: Scale `kitos-api` replicas based on load

## Architecture

```
Internet
    │
    ▼
┌─────────────────┐
│  nginx Ingress  │
│  (kitos.example)│
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌────────┐  ┌──────────┐
│kitos-api│  │pubsub-api│
│ :8080   │  │  :8080   │
└──┬───┬──┘  └──┬───┬───┘
   │   │        │   │
   ▼   ▼        ▼   ▼
┌────┐ ┌────────┐ ┌────┐
│ PG │ │RabbitMQ│ │ PG │
└────┘ └────────┘ └────┘
```
