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

## Parameters (write models)
Write services never accept API DTOs. Inputs are API-agnostic parameter models in `Core.ApplicationServices/Model/{Domain}/Write/`, built by the controller's write model mapper (`.agents/skills/kitos-controllers/SKILL.md`). Reference: `Model/SystemUsage/Write/`.
- **Naming**: `{Domain}UpdateParameters` (root), `Updated{Domain}{Section}Properties|Parameters` (sections), `{Domain}CreationParameters` (create), `{SubResource}Parameters` / `…Properties` (sub-resource commands, e.g. `SystemRelationParameters`, `SystemUsageJournalPeriodProperties`).
- **Root update model**: one `Maybe<TSection>` property per section, defaulting to `Maybe<TSection>.None`. `None` = section untouched.
- **Section models**: one `OptionalValueChange<T>` per field, defaulting to `OptionalValueChange<T>.None`. `None` = no change; `HasChange` + `NewValue` = set. Clearable values use `OptionalValueChange<Maybe<T>>` (or a nullable `T?` for enums/value types) so "set to empty" differs from "unchanged". References to other entities are UUIDs (`Maybe<Guid>`, `Maybe<IEnumerable<Guid>>`), never DB ids or entities.
- **Creation**: `{Domain}CreationParameters` holds the mandatory create-only identifiers (immutable, ctor-assigned) plus `{Domain}UpdateParameters AdditionalValues`; `Create` creates the entity and then applies `AdditionalValues` through the same update path as `Update`.
- **Sub-resource commands**: small immutable classes (ctor + get-only properties) or `required` init properties when every value is mandatory.
- Domain enums only; API `*Choice` enums are mapped in the presentation layer.
- **Applying** in the write service: chain section and field updates with `WithOptionalUpdate`, inside one transaction, stopping on the first error:
  ```csharp
  return entity.WithOptionalUpdate(parameters.GeneralProperties, PerformGeneralUpdate)
      .Bind(e => e.WithOptionalUpdate(parameters.Roles, PerformRoleUpdates));
  ```
  Use `MapOptionalChangeWithFallback` / `MapDateTimeOptionalChangeWithFallback` when a change must be merged with the current value for validation.
- When field-level authorization applies (supplier-associated fields), implement `ISupplierAssociatedEntityUpdateParameters.GetChangedPropertyKeys()` on the root parameters and keep it in sync with new fields; it is consumed by `SupplierAssociatedFieldsService`.
- Unit-test the service with hand-built parameters covering `None`, change-to-value and change-to-empty per field.

## Tests
- `{ClassUnderTest}Test` extending `WithAutoFixture`, Moq for dependencies, `MethodName_Condition_ExpectedResult` names.
- Cover success, each failure branch, and authorization.

## Finish
Run `pwsh ./scripts/quality-check.ps1` and review against `.agents/skills/code-quality/SKILL.md`.
