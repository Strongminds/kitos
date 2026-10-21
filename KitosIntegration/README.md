# KitosIntegration

Standalone ASP.NET Core API with one operation: `POST /api/v2/integration/beskedfordeler`.

Open `Kitos_IntegrationApi.sln` for the API, service layer, infrastructure and dedicated tests. These projects are separate from `KITOS.sln`. Only `Kitos.Integration.Contracts` is referenced by both solutions; it contains message metadata, schemas, status values and the serialized job interface, with no service implementations.

The dependency direction is **Presentation → Services → Infrastructure**:

- `KitosIntegration` is the presentation host: controllers, XML response mapping, HTTP certificate authentication, TLS configuration and dependency injection.
- `KitosIntegration.Services` owns the bounded receive workflow, envelope validation, message classification and acknowledgement decisions. `IBeskedfordelerService` lives alongside `BeskedfordelerService`.
- `KitosIntegration.Infrastructure` owns schema-based XML reading, certificate validation and durable Hangfire enqueue. Its reader, validator and repository interfaces live alongside their implementations. Infrastructure does not reference Services or Presentation.

The presentation composition root references infrastructure to wire implementations; controllers depend on service interfaces. The shared `Kitos.Integration.Contracts` project remains the cross-process wire contract. Its `IBeskedfordelerJob` interface retains its assembly and type identity so queued jobs remain readable. KITOS implements that contract in `Infrastructure.STS.Beskedfordeler`, consuming the queue and performing organization lookup, ingestion and administrator review. No services are shared between solutions.

`Tests.Unit.KitosIntegration` tests the integration API, service workflow and infrastructure. KITOS worker, municipality mapping, replay and compatibility tests remain in `Tests.Unit.Presentation.Web`, which has no reference to the integration API projects.

This host registers no Hangfire server, dashboard, KITOS user services or application database. Its publish output excludes the worker and KITOS core, SAML and SOAP dependencies. KITOS retains both original concrete job identities for previously queued jobs, and registers the interface for new deliveries. Deploy the updated KITOS worker before starting this receiver version; older workers cannot execute the new interface contract.

Publishing retains only Danish satellite resources (`da`/`da-DK`), with neutral resources retained for dependencies that have no Danish translation. Remaining runtime DLLs support the API, Hangfire and PostgreSQL; PDBs support diagnostics. Publish into an empty output directory when changing dependencies: `dotnet publish` does not remove obsolete files from previous publishes.

## Configuration

Supply configuration through the deployment environment or a protected settings file:

| Environment variable | Value |
| --- | --- |
| `ConnectionStrings__kitos_HangfireDB` | The same PostgreSQL database used by KITOS, with schema `hangfire`. |
| `AppSettings__BeskedfordelerCertificateThumbprint` | Comma-separated allowed client certificate SHA-1 thumbprints, 40 hexadecimal characters each. |
| `Beskedfordeler__SchemaDirectory` | Optional complete external XSD directory; omit to use embedded schemas. Keep any override consistent with KITOS. |

Configure the HTTPS server certificate through Kestrel or IIS. Kestrel requests client certificates during TLS 1.2/1.3 negotiation; HTTP authentication requires an allowlisted certificate with a valid chain, client-authentication usage and online revocation checks. Install the relevant trust chain and enable CRL/OCSP access. Missing certificates receive 403; TLS itself may reject invalid certificates before HTTP. Cookies, bearer tokens and caller-supplied certificate headers do not authenticate requests.

For IIS, use a separate site/application with HTTPS and `SslNegotiateCert`, preferably in-process hosting, and verify the authenticated connection certificate reaches ASP.NET Core. Do not substitute arbitrary forwarded-certificate headers. KITOS itself no longer requires the callback thumbprint setting or negotiates client certificates.

## Build and deployment

```powershell
dotnet build Kitos_IntegrationApi.sln -c Release
dotnet test Tests.Unit.KitosIntegration/Tests.Unit.KitosIntegration.csproj -c Release
dotnet publish KitosIntegration/KitosIntegration.csproj -c Release -o artifacts/KitosIntegration
dotnet run --project KitosIntegration/KitosIntegration.csproj
```

Provide the settings and HTTPS binding before running. The receiver uses KITOS's Hangfire serializer settings and does not create the database or migrate its schema. Start KITOS first to initialize storage. Publish the API separately; the existing Presentation.Web deployment package does not deploy it.

Deploy from the repository checkout with PowerShell 7, the .NET 10 SDK, AWS CLI and IIS Web Deploy installed:

```powershell
./DeploymentScripts/DeployKitosIntegrationToEnvironment.ps1 -targetEnvironment integration -iisApplication 'KitosIntegration' -WhatIf
./DeploymentScripts/DeployKitosIntegrationToEnvironment.ps1 -targetEnvironment integration -iisApplication 'KitosIntegration'
```

Set `AwsAccessKeyId` and `AwsSecretAccessKey` as for standard KITOS. The script reuses its AWS helper, region and `/kitos/<environment>/` SSM path, including `MsDeployUrl`, `MsDeployUserName`, `MsDeployPassword` and `HangfireDbConnectionStringForIIsApp`. Supply `BeskedfordelerCertificateThumbprint` in that same path. The receiver requires PostgreSQL. No separate AWS namespace or deployment credentials are needed.

The IIS destination is mandatory: provision a separate site/application and app pool with the .NET 10 Hosting Bundle, HTTPS binding and client certificate negotiation described above. The shared Web Deploy user must have access to that destination. `Default Web Site` alone is rejected to protect the KITOS backend. The script publishes into a fresh temporary directory, stamps the receiver settings, syncs through Web Deploy with AppOffline, and removes the temporary files even on failure. `-WhatIf` builds and loads configuration but only previews the remote sync. It does not provision IIS, certificates or the database.

For cutover, pause upstream delivery and deploy the updated KITOS worker first. Then deploy/configure the new receiver against the existing Hangfire database, verify its certificate handshake, update the Beskedfordeler callback host (the route is unchanged), and resume delivery. Verify a real business message produces a pending item in KITOS. Preserve shared Hangfire storage and accepted jobs during cutover or rollback; never point the new receiver at a separate empty database.

Business messages retain protocol statuses and durable enqueue behavior, including status 53 on handoff failure. Connectivity test messages retain the existing immediate log-and-acknowledge behavior without enqueue. KITOS retains retries, replay idempotency, failed jobs and administrator review. Monitor the shared queue using KITOS's protected Hangfire administration. Perform target-environment certificate rotation/revocation and end-to-end delivery acceptance before production activation.
