---
name: kitos-services
description: Create or refactor services in KITOS (application, domain, and infrastructure/integration services) following repository conventions for readability, Result/Maybe, sync-first code, transactions, and authorization. Use when adding a new service or reviewing service structure.
---

# Creating services in KITOS

## Placement
- Business logic: `Core.ApplicationServices` (`I{Domain}Service` / `I{Domain}WriteService`); contracts for infrastructure-backed services in `Core.DomainServices`.
- Integrations: `Infrastructure.STS.<Service>` (see `.agents/skills/kombit-services/SKILL.md`). Use the nearest sibling service as a template, e.g. `StsOrganizationService`.
- Register in the active composition root (`Presentation.Web/Infrastructure/DI/KitosServiceRegistration.cs`, or a dedicated registration extension) and in Ninject where the legacy host still needs it.

## Structure
- Constructor injection of interfaces only; no `new` of collaborators.
- Public methods read as a short sequence of steps. Extract each step (read, validate, map, persist, enqueue) into a small private method with an intention-revealing name; no long linear bodies. Example: `BeskedfordelerService.Receive`.
- Named `const` fields for limits, versions, buffer sizes; no inline literals.
- Comments only for non-obvious rationale (security, protocol quirks).

## Conventions
- **Sync by default.** KITOS does not use async. Use `async` only when the platform forces it (e.g. ASP.NET Core request body streams reject synchronous reads);
- Return `Result<T, OperationError>` for failures and `Maybe<T>` for optionals; never throw for business failures. Protocol services may return a status enum when the wire contract requires it.
- Check permissions via `IAuthorizationContext` before mutating.
- Use `ITransactionManager` for write boundaries; publish `IDomainEvents` from services, not entities.
- Repositories: `.agents/skills/kitos-repositories/SKILL.md`.
- Catch only expected exception types; rethrow `OperationCanceledException`; never log payloads or secrets.

## Tests
- `{ClassUnderTest}Test` extending `WithAutoFixture`, Moq for dependencies, `MethodName_Condition_ExpectedResult` names.
- Cover success, each failure branch, and authorization.

## Finish
Run `pwsh ./scripts/quality-check.ps1` and review against `.agents/skills/code-quality/SKILL.md`.
