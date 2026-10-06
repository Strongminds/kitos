---
name: code-quality
description: "Review and improve KITOS C# code quality. Use after making code changes, before marking a PR ready, or when asked to review/clean up code. Runs the KITOS quality gate and checks KITOS-specific conventions that analyzers cannot enforce."
---

# KITOS code quality review

## 1. Run the automated gate

```
pwsh ./scripts/quality-check.ps1
```

Iterate until it reports `Quality gate PASSED`:

| Result | Action |
|---|---|
| `Format FAIL` | `pwsh ./scripts/quality-check.ps1 -Fix -SkipBuild -SkipTests` |
| `Build FAIL` | Fix every reported warning/error at its root. Never suppress or relax rules to pass. |
| `No debug output FAIL` | Remove `Console.Write*` / `Debug.Write*` / `Debugger.Break` – use injected Serilog `ILogger`. |
| `Justified suppressions FAIL` | Remove the suppression, or add a `// reason` / `Justification = "..."` and tell the user. |
| `Migration consistency FAIL` | Regenerate the migration so `.Designer.cs` and `KitosContextModelSnapshot.cs` are included. |
| `Tests accompany changes WARN` | Add/adjust unit tests (see section 3). |
| `Database compatibility WARN` | Ensure SQL Server and PostgreSQL variants exist and match. |
| `Analyzer baseline not extended WARN` | Revert the relaxation unless explicitly agreed with the user. |
| `Unit tests FAIL` | Fix the code (or the test if the requirement changed – say so). |

## 2. Review the diff against KITOS conventions

Run `git diff origin/master...HEAD` (plus uncommitted changes) and check each changed file:

### Error handling
- [ ] Business failures return `Result<T, OperationError>` / `Maybe<T>` – no exceptions for expected failures.
- [ ] `OperationFailure` matches the semantics (`NotFound`, `Forbidden`, `Conflict`, `BadInput`).
- [ ] No `null` returned where `Maybe<T>` is the convention; results are not unwrapped with `.Value` without checking.

### Authorization & security
- [ ] Reads/writes are guarded via `IAuthorizationContext` **before** mutating or returning data.
- [ ] Organization scoping respected (no cross-organization data leaks in queries/read models).
- [ ] No raw SQL built from user input; no secrets, connection strings or personal data in code/logs.
- [ ] Public API (`V2/External`, `[PublicApi]`) changes are backwards compatible.

### Persistence & transactions
- [ ] Write operations use `ITransactionManager` (`Begin()` / `Commit()`); no stray `SaveChanges()` outside the established `IDatabaseControl` pattern.
- [ ] Domain events (`EntityCreatedEvent<T>`, `EntityUpdatedEvent<T>`, `EntityBeingDeletedEvent<T>`) raised from application services.
- [ ] Schema changes go through a migration; read models updated if list/overview data changed.
- [ ] No N+1 queries or unbounded `ToList()` on large sets; list endpoints prefer read models.

### Structure & naming
- [ ] Layering respected: `Core.DomainModel` ← `Core.DomainServices` ← `Core.ApplicationServices` ← `Presentation.Web`.
- [ ] Controllers are thin; mapping in `IXxxResponseMapper` / `IXxxWriteModelMapper`.
- [ ] Naming: `{Domain}V2Controller`, `{Domain}InternalV2Controller`, `I{Domain}Service`, `I{Domain}WriteService`, private fields `_camelCase`.
- [ ] Dependencies injected via constructor and registered in `Infrastructure.Ninject` / DI setup.

### Clean code
- [ ] Methods are small and single-purpose; no duplicated logic (extract or reuse existing helpers/extensions).
- [ ] No dead code, commented-out code, unused usings/parameters/private members in touched files.
- [ ] Nullable annotations are honest (no `!` to silence warnings without reason).
- [ ] Comments explain *why*, not *what*.

## 3. Tests

- Unit tests extend `WithAutoFixture`; use `A<T>()` / `Many<T>()`, Moq mocks injected in the constructor.
- Name: `MethodName_Condition_ExpectedResult` (or the existing `Can_.../Cannot_...` style in the same class).
- Cover the success path **and** each failure branch (`Forbidden`, `NotFound`, `BadInput`, …) of changed logic.
- Controller/API contract changes: add or update integration tests in `Tests.Integration.Presentation.Web`.

## 4. Report

Summarize for the user: gate result, issues fixed, any remaining `WARN`s and why they are acceptable.
