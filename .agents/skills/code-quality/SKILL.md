---
name: code-quality
description: Review KITOS C# changes using the repository quality gate and conventions for authorization, persistence, API contracts, and tests. Use for code reviews, code cleanup, and validation before marking implementation work complete.
---

# KITOS code quality

Review the requested change against its intended base branch, including relevant staged, unstaged, and untracked files. The quality gate defaults to `origin/master`; use `-BaseRef` when the actual target differs. For review-only requests, report findings rather than modifying code unless fixes are authorized.

## Validation

For implementation changes, run from the repository root:

```powershell
pwsh ./scripts/quality-check.ps1
```

The script checks formatting, builds both solutions, checks diff conventions, and runs unit tests. Consult `scripts/quality-check.ps1` and `docs/CODE_QUALITY.md` for current behavior. Use `-SkipBuild` or `-SkipTests` during iteration only; a partial run is not a full pass. Documentation-only and skill-only changes need structural and reference checks, not application builds.

Fix failures introduced by the change and rerun affected checks. Do not suppress diagnostics, weaken analyzers, or change tests merely to obtain a pass. If a failure is pre-existing or caused by unavailable infrastructure or dependencies, record the evidence and validation limitation instead of expanding into unrelated repairs or retrying indefinitely. Do not claim the gate passed when it did not.

Interpret heuristic warnings before acting:

- Format failures: use `-Fix -SkipBuild -SkipTests` where appropriate and inspect the resulting diff.
- Debug output: remove accidental diagnostics from production paths; preserve intentional CLI output and use the affected application's logging conventions.
- Suppressions and analyzer changes: assess the reason and scope; do not add them to hide a defect. Explain any intentional exception.
- Missing tests: add tests for meaningful behavior and risks, not simply to satisfy a changed-file heuristic.
- Database compatibility: verify the providers supported by the affected code path. Require SQL Server and PostgreSQL variants only for scripts that actually support both.
- Migration consistency: inspect the affected context, migration, designer, and final snapshot together. The gate's KITOS check does not cover every context or prove schema equivalence. For a consolidation with an unchanged final model, an unchanged snapshot can be correct; investigate the warning rather than manufacturing a snapshot change.

## KITOS review checks

### Business behavior and authorization

- Follow `Result<T, OperationError>` and `Maybe<T>` where established. Check failure values before unwrapping, and map `OperationFailure` to the intended API response.
- Preserve authorization at service/API boundaries and organization scoping in queries. Avoid duplicate authorization machinery inside repositories.
- Preserve public API compatibility, including response mapping and validation. Keep controllers thin and use existing mapper contracts.
- Check SQL parameterization and avoid exposing credentials or sensitive user data in output and logs.

### Persistence

For repository changes, read `.agents/skills/kitos-repositories/SKILL.md` (path relative to the repository root). Prefer `IGenericRepository<T>` or `GenericRepository<T>` whenever possible.

Preserve save and transaction boundaries, tracking choices, concurrency handling, and domain events. Use the established transaction abstraction for the affected service; specialized infrastructure operations may require direct context access. Do not force KITOS abstractions onto PubSub's separate context.

Inspect EF Core migrations in the correct location:

- KITOS: `Infrastructure.DataAccess/Migrations/EfCore/`, with `KitosContextModelSnapshot.cs`.
- PubSub: `PubSub.Infrastructure.DataAccess/Migrations/`, with `PubSubContextModelSnapshot.cs`.

Validate upgrade and rollback behavior when changing migrations. Keep generated metadata consistent with the final schema. Historical EF6 migrations are not the active migration workflow. Check provider assumptions against the actual context configuration.

Review query materialization, pagination, eager loading, and N+1 risks. Keep affected overview/read models and their update paths consistent with domain changes.

### Structure and dependency injection

Respect layering between domain model, domain services, application services, infrastructure, and presentation. Register dependencies in the affected application's active composition root; KITOS registrations are in `Presentation.Web/Infrastructure/DI/KitosServiceRegistration.cs`. Check lifetimes and shared context ownership. Do not default to legacy `Infrastructure.Ninject` merely because that project remains in the solution.

Follow nearby naming and nullable conventions. Remove defects and obsolete code in the changed area without broad unrelated refactors. Comments should explain non-obvious reasons and constraints.

## Tests and reporting

Use existing relevant tests first. Add focused tests for changed behavior, especially organization isolation, authorization, API compatibility, duplicate delivery, and concurrency when affected. Follow nearby xUnit/Moq conventions; use `WithAutoFixture` where helpful, without requiring it for every test. Use integration or container tests when the behavior depends on real HTTP or database semantics, and report unavailable prerequisites.

For reviews, report actionable findings with file/line references and practical consequences. For fixes, summarize changes and validation. Distinguish passed checks, skipped checks, remaining warnings, and blockers; do not present an unrun test or a heuristic check as proof of correctness.
