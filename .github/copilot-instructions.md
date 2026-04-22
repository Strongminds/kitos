# OS2KITOS – Copilot Instructions

OS2KITOS is a Danish municipal IT portfolio management platform. It helps public organizations manage IT systems, contracts, interfaces, and data processing registrations (GDPR). Maintained by STRONGMINDS ApS for OS2.

The legacy UI (AngularJS) lives in this repo. The **new frontend** is maintained separately: https://github.com/os2kitos/kitos_frontend

---

## Build & Test

Build the solution with MSBuild or Visual Studio:
```
msbuild KITOS.sln
```

**Unit tests** (xUnit, no external dependencies):
```
dotnet test Tests.Unit.Core.ApplicationServices
dotnet test Tests.Unit.Presentation.Web
```

Run a single unit test class:
```
dotnet test Tests.Unit.Core.ApplicationServices --filter "FullyQualifiedName~ItSystemWriteServiceTest"
```

**Integration tests** require a running KITOS instance and SQL Server at `.\SQLEXPRESS` (database: `Kitos`). Set `KitosTestEnvironment=Local` (default if unset). Integration tests run sequentially within `[Collection(nameof(SequentialTestGroup))]`.

**EF6 migrations** are in `Infrastructure.DataAccess/Migrations/`. Run via Package Manager Console:
```
Update-Database -ProjectName Infrastructure.DataAccess
```

---

## Architecture

### Layer structure (inner → outer)

| Project | Purpose |
|---|---|
| `Core.Abstractions` | Shared functional types: `Result<T,E>`, `Maybe<T>`, `OperationError`, `OperationFailure` |
| `Core.DomainModel` | Entity classes, domain events, interfaces |
| `Core.DomainServices` | Repository interfaces, domain service contracts |
| `Core.ApplicationServices` | Business logic: read/write services, authorization, mapping models |
| `Core.BackgroundJobs` | Hangfire scheduled jobs |
| `Infrastructure.DataAccess` | EF6 `KitosContext`, `GenericRepository<T>`, migrations |
| `Infrastructure.Ninject` | DI bindings (Ninject) |
| `Infrastructure.STS.*` | SAML/KOMBIT STS integration (Danish government SSO) |
| `Infrastructure.OpenXML` | Excel export |
| `Presentation.Web` | ASP.NET Web API + MVC host; V1 and V2 API controllers, legacy AngularJS UI |
| `Tests.Unit.Core.ApplicationServices` | Unit tests for application services |
| `Tests.Integration.Presentation.Web` | HTTP-level integration tests |
| `Tests.Toolkit` | Shared test base classes |

### API versioning

Controllers live under `Presentation.Web/Controllers/API/`:
- **V1** – legacy API, used by the old AngularJS UI
- **V2/External/** – public API, decorated with `[PublicApi]`, inherits `ExternalBaseController`
- **V2/Internal/** – internal API for the new frontend, inherits `InternalApiV2Controller`

Each V2 domain area has paired mapper types:
- `IXxxResponseMapper` / `XxxResponseMapper` – domain entity → API response DTO
- `IXxxWriteModelMapper` / `XxxWriteModelMapper` – API request DTO → domain write model

### Database

EF6 Code First with SQL Server. `KitosContext` is the single `DbContext`. Access goes through `IGenericRepository<T>` → `GenericRepository<T>`. All schema changes are done via EF migrations.

### Option types

There are **Global** option types (system-wide, managed by global admins) and **Local** option types (per-organization customizations). Both follow a `OptionEntity` → `LocalOptionEntity` inheritance pattern. Controllers for these follow the `BaseGlobalRegularOptionTypesInternalV2Controller` / `BaseLocalRegularOptionTypesInternalV2Controller` base class pattern.

---

## Key Conventions

### Error handling: `Result` and `Maybe` monads

Never throw exceptions for business logic failures. Application services return `Result<TSuccess, OperationError>`:
```csharp
Result<ItSystem, OperationError> result = _service.GetSystem(uuid);
return result.Match(
    onSuccess: system => Ok(responseMapper.Map(system)),
    onFailure: error => FromOperationError(error)
);
```

`OperationFailure` enum maps directly to HTTP status codes: `NotFound` → 404, `Forbidden` → 403, `Conflict` → 409, `BadInput` → 400.

Use `Maybe<T>` for optional values (instead of `null`). Implicit conversion from `T` works: `T value = null` becomes `Maybe<T>.None`.

### Unit test pattern

Unit test classes extend `WithAutoFixture` from `Tests.Toolkit`. Use `A<T>()` to create test data and `Many<T>()` for collections. Mock with Moq; inject via constructor in `public TestClassName()`.

```csharp
public class MyServiceTest : WithAutoFixture
{
    private readonly MyService _sut;
    private readonly Mock<IDependency> _depMock;

    public MyServiceTest()
    {
        _depMock = new Mock<IDependency>();
        _sut = new MyService(_depMock.Object);
    }
}
```

Test methods follow the `MethodName_Condition_ExpectedResult` naming pattern (e.g., `Can_GET_Public_ItSystem_As_Stakeholder_If_Placed_In_Other_Organization`).

### Transactions

Use `ITransactionManager` for explicit transaction boundaries in write operations. Services receive it via constructor injection and call `_transactionManager.Begin()` / `_transactionManager.Commit()`. Do not call `SaveChanges()` directly — let the transaction manager handle it.

### Domain events

Domain changes publish events via `IDomainEvents`. Common events: `EntityCreatedEvent<T>`, `EntityUpdatedEvent<T>`, `EntityBeingDeletedEvent<T>`. Raise events from application services, not domain entities directly.

### Authorization

Authorization goes through `IAuthorizationContext` (injected into services). Check permissions before mutating entities. `IOrganizationalUserContext` provides the current user's organizational role and memberships.

### Read models

For complex queries (e.g., grid/list views), denormalized read models (e.g., `ItSystemUsageOverviewReadModel`, `ItContractOverviewReadModel`) live in `Core.DomainModel` and are kept up-to-date via background jobs. Prefer read models over ad-hoc joins for list endpoints.

### Naming

- Controllers: `{Domain}V2Controller` (external) or `{Domain}InternalV2Controller` (internal)
- Services: `I{Domain}Service` / `I{Domain}WriteService`
- Mappers: `I{Domain}ResponseMapper` / `I{Domain}WriteModelMapper`
- Tests: `{ClassUnderTest}Test`

### Danish domain terms

- **KLE** – *Kommunernes Landsforenings Emnesystem*: task/topic classification system used by Danish municipalities
- **DPR** – Data Processing Registration (GDPR data processor agreements)
- **STS** – *Security Token Service*: KOMBIT's Danish government SSO/federated identity
- **ItSystemUsage** – an organization's local registration/usage of a globally defined `ItSystem`
