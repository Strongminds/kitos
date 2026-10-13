---
name: kombit-services
description: Add, refactor, or expose KOMBIT integrations in KITOS, including STS organization services and Beskedfordeler callbacks. Use for KOMBIT contracts, schemas, authentication, service registration, and integration endpoints.
---

# KOMBIT services in KITOS

Keep KOMBIT protocol code alongside the existing Infrastructure.STS.* services. Read the nearest integration before choosing dependencies; these paths are relative to the repository root.

- Infrastructure.STS.Organization/DomainServices/StsOrganizationService.cs shows an outbound service implementing a core contract.
- Infrastructure.STS.Common/Model/Token/ owns shared token and certificate support; Model/Client/RetriedIntegrationRequest.cs provides the outbound retry pattern.
- Infrastructure.STS.SvcUtilClients/ owns generated SOAP clients and their WSDL/XSD inputs. Preserve generated contracts and regenerate from the applicable contract when necessary.
- Infrastructure.STS.Beskedfordeler/ owns inbound callback schemas, XML validation, certificate validation, event mapping, and durable handoff/processing.

## Placement and exposure

Put service-specific wire contracts, schemas, parsing, and integration implementations in the appropriate infrastructure project. Add a sibling Infrastructure.STS.<Service> project for a distinct integration. Do not put KOMBIT XSDs or integration processing in Presentation.Web.

Expose inbound integration HTTP operations through a thin controller under Presentation.Web/Controllers/API/V2/Integration/ inheriting IntegrationBaseController. The inherited IntegrationApi attribute selects the dedicated certificate authentication scheme; do not inherit ExternalBaseController or bypass authorization with AllowAnonymous. Inject the integration service; keep HTTP response construction in the controller and protocol work in infrastructure. Preserve required KOMBIT wire responses rather than replacing them with ordinary JSON. Outbound domain-facing service interfaces and models belong in core; do not expose generated SOAP types through KITOS API DTOs.

Register services in the active ASP.NET Core composition root (Presentation.Web/Infrastructure/DI/KitosServiceRegistration.cs or a dedicated registration extension invoked from Program.cs). Legacy Ninject registrations alone do not activate services in the current host. Add new projects to KITOS.sln and verify schemas reach consuming build and publish outputs with their expected paths.

## Authentication and delivery invariants

Reuse the existing token/client setup for outbound calls. A Beskedfordeler callback uses the TLS client-certificate handshake and allowlisted, chain-validated client certificates through IntegrationCertificateAuthenticationHandler. Read certificates from the authenticated connection, never arbitrary request headers. Select certificate authentication before the cookie/JWT flow and exclude integration endpoints from KITOS user-context middleware and user-role filters. Bearer tokens and cookies must neither grant integration access nor interfere with a valid certificate. The shared HTTPS listener requests client certificates with AllowCertificate; the integration scheme requires one for each integration request without imposing mTLS on ordinary user endpoints. When IIS terminates TLS, configure certificate negotiation there and expose the certificate through the trusted server connection feature. Keep secrets and real payloads out of source and logs.

For Beskedfordeler, bound the request body, prohibit DTDs and external XML resolution, and validate against the complete bundled schema set. Acknowledge acceptance only after durable enqueue. Preserve exact original bytes, replay idempotency, retry behavior, and visible failures for unsupported events. Preserve serialized Hangfire type/method identities when moving job classes, using a compatibility adapter when existing jobs may still reference the old assembly.

Keep CVR-to-KITOS organization mapping explicit. An event's actor is not necessarily the affected user. Receipt of a deactivation creates an administrator-reviewed change; it does not itself remove access. Read docs/technical-documentation/architecture-decision-log/fk-org-users-integration.md for this flow's mapping and deployment constraints.

Validate changed behavior with focused service and controller tests, including a real HTTPS client-certificate handshake, missing/invalid certificates, token-flow isolation, disabled requests, invalid XML, enqueue failures, and replay when affected. Follow the repository code-quality skill for the final gate; distinguish local tests from live KOMBIT acceptance.
