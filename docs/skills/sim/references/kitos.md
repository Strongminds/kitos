# KITOS implementation context

Read only for work in OS2KITOS. Paths below are repository-relative and must be inspected for current instructions; this reference does not replace the project's current conventions.

## Development conventions

- Read `README.md`, `.github/copilot-instructions.md`, `docs/CODE_QUALITY.md`, and `.github/skills/code-quality/SKILL.md`. Apply the code-quality skill after changes. Read `CONTAINERIZATION.md` for local runtime setup.
- Preserve the existing core/domain/application/presentation boundaries. Thin controllers use dedicated request/response mappers and injected services. Use `Result<T, OperationError>` for expected failures, `Maybe<T>` for optional values, `IAuthorizationContext` for permissions, and existing transaction/domain-event patterns for writes. Independently deployed components need not inherit KITOS infrastructure dependencies merely to resemble existing services.
- New persistence work targets PostgreSQL with EF Core/Npgsql only. Historical EF6/SQL Server files are not compatibility requirements. Current mappings and migrations are in `Infrastructure.DataAccess/Mapping/` and `Infrastructure.DataAccess/Migrations/EfCore/`. Include migration designer/snapshot files when needed. Applying migrations to an existing database requires explicit authorization under current repository instructions; distinguish this from initializing an explicitly authorized disposable fixture.
- Unit tests use xUnit, `WithAutoFixture`, AutoFixture, and Moq. Follow neighboring naming/fixture patterns and test observable behaviors.

## Required checks and runtime

The final quality gate is `pwsh ./scripts/quality-check.ps1`, including tests. It builds both solutions and runs unit suites, but does not run HTTP integration or container tests. Run relevant additional suites explicitly:

```powershell
dotnet test Tests.Integration.Presentation.Web
dotnet test Tests.Container
```

Select appropriate filters during iterations, then run the required final coverage. Verify current project paths and prerequisites before execution. The integration suite requires a running KITOS instance and PostgreSQL. Confirm `KitosTestEnvironment=Local` and an explicitly identified disposable test instance: fixtures create users, organizations, and registrations. Do not print configured secrets while verifying the target.

Use ordinary/scoped test identities for actual acceptance calls; admin fixture setup cannot prove least-privilege behavior. Coordinate shared-state tests and respect sequential collections. Read models update asynchronously: use existing bounded readiness helpers instead of arbitrary sleeps. Container tests require Docker/Podman and appropriate cleanup.

The current frontend is in a separate repository; this backend repository also contains a legacy UI. Inspect the UI relevant to the changed feature. Headless HTTP/MCP calls and database assertions are usually stronger evidence for backend work than browsing an unrelated page.

## Current remote MCP design

For `docs/designs/kitos-mcp-server.md`, retain the agreed remote-only first-release scope and the approved Architecture subsections. Read the document again at each iteration; do not infer full implementation authorization from the existence of the draft.

The full document still contains open choices and broad model coverage. Establish an agreed MVP subset and acceptance scenarios before treating it as a ready-to-implement chunk. Remote scope alone does not settle credential isolation, caller bootstrap, model coverage, or snapshot requirements.

Mandatory security acceptance includes GET-only KITOS data access enforced outside the MCP process, caller-bound JWT permissions, and credentials visible to MCP that cannot be replayed for writes. MCP's HTTP POST transport does not conflict with downstream GET-only data access. A normal KITOS JWT with write rights is not read-only; do not hand it or KITOS's HMAC signing secret to the MCP process. Keep MCP and downstream credentials distinct.

Inspect real MCP tool calls and rejected plans, ordinary/denied users, organization isolation, bounded pagination/fan-out, and consistency/completeness warnings. Observe or instrument the actual upstream boundary to verify methods/routes and write denial; mocks or MCP read-only annotations alone are insufficient. Controlled concurrent edits belong in disposable test fixtures, not permission to add MCP write tools or modify live registrations.
