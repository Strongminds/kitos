---
name: dotnet-senior-dev
description: >
  Senior .NET developer guide for the KITOS codebase. Use this skill when asked to implement
  features, fix bugs, write tests, create API endpoints, add domain entities, create EF6
  migrations, or review C# code in this project.
---

# KITOS Senior .NET Developer Skill

## Project Overview

KITOS is a Danish municipal IT portfolio management platform targeting **net10.0**, built on
ASP.NET Core (Web API + MVC), Entity Framework 6 (Code First), and a DDD-influenced layered
architecture. The legacy UI is AngularJS; the new frontend is a separate React app.

---

## Architecture Layers (inner → outer)

| Layer | Project |
|---|---|
| Shared types | `Core.Abstractions` |
| Domain entities & events | `Core.DomainModel` |
| Repository & service contracts | `Core.DomainServices` |
| Business logic | `Core.ApplicationServices` |
| Scheduled jobs | `Core.BackgroundJobs` |
| EF6 DbContext & migrations | `Infrastructure.DataAccess` |
| DI bindings | `Infrastructure.Ninject` *(legacy)* / `Presentation.Web/Infrastructure/DI` |
| ASP.NET host + controllers | `Presentation.Web` |

**Dependency rule**: outer layers depend on inner layers. Never import `Presentation.Web` or
`Infrastructure.*` from `Core.*`.

---

## Error Handling — Result and Maybe Monads

**Never throw exceptions for business logic failures.**

Application services return `Result<TSuccess, OperationError>`:

```csharp
// Failure
return new OperationError("Reason", OperationFailure.NotFound);

// Success
return Result<MyDto, OperationError>.Success(dto);

// Chaining — use .Bind() and .Select()
return _repository.GetById(id)
    .Match(
        onNone: () => new OperationError("Not found", OperationFailure.NotFound),
        onSome: Result<Entity, OperationError>.Success)
    .Bind(entity => Validate(entity))
    .Select(entity => MapToDto(entity));
```

Use `Maybe<T>` for nullable lookups:

```csharp
Maybe<User> user = _userRepository.GetById(id);
if (user.IsNone) return new OperationError(...);
```

---

## Domain Entities

- Inherit from `Entity` (has `Id`) or `HasRightsHolder` where appropriate.
- Use **domain events** for cross-aggregate side effects — raise via `IDomainEvents.Raise(...)`.
- Keep setters `private` or `internal`; mutation goes through domain methods.
- Value objects are records or small structs.

---

## Repository Pattern

- All DB access through `IGenericRepository<T>` — never use `KitosContext` directly in
  application services.
- Repository calls return `Maybe<T>` for single-item lookups and `IQueryable<T>` for
  collections.
- Persistence is committed by the unit of work at the controller/middleware boundary — do
  **not** call `Save()` inside application services.

---

## API Controllers

### V2 External (Public API)

- Located in `Presentation.Web/Controllers/API/V2/External/`
- Inherit `ExternalBaseController`
- Decorated with `[PublicApi]`
- Use paired mapper types: `IXxxResponseMapper` / `IXxxWriteModelMapper`

### V2 Internal (New Frontend)

- Located in `Presentation.Web/Controllers/API/V2/Internal/`
- Inherit `InternalApiV2Controller`

### Implementing a new V2 endpoint

1. Add the route method to the controller and call the application service.
2. Map the domain result to a response DTO via the response mapper.
3. Return `FromOperationError(error)` for failures; `Ok(dto)` for success.
4. Register write model mapper and response mapper in `KitosServiceRegistration.cs`.

```csharp
[HttpGet("{uuid}")]
public IActionResult Get(Guid uuid)
{
    return _service.GetByUuid(uuid)
        .Select(_responseMapper.MapToResponseDto)
        .Match(Ok, FromOperationError);
}
```

---

## Option Types (Global vs Local)

- **Global options**: system-wide, managed by global admins. Controllers extend
  `BaseGlobalRegularOptionTypesInternalV2Controller`.
- **Local options**: per-organisation. Controllers extend
  `BaseLocalRegularOptionTypesInternalV2Controller`.

---

## EF6 Migrations

All schema changes **must** go through EF6 Code First migrations. Never alter the database
directly.

```powershell
# Add a new migration (run in Package Manager Console, target Infrastructure.DataAccess)
Add-Migration -Name <MigrationName> -ProjectName Infrastructure.DataAccess

# Apply pending migrations
Update-Database -ProjectName Infrastructure.DataAccess
```

Migration files live in `Infrastructure.DataAccess/Migrations/`.

When mapping a new entity:
1. Add the `DbSet<T>` to `KitosContext`.
2. Configure it in `KitosContext.OnModelCreating` (or a separate `EntityTypeConfiguration<T>`).
3. Add a migration and inspect the generated SQL before applying.

---

## Unit Tests

**Framework**: xUnit + Moq. No external dependencies (no database, no HTTP).

Naming convention: `MethodName_Scenario_ExpectedBehavior`

```csharp
[Fact]
public void CreateUser_WhenEmailAlreadyExists_ReturnsConflict()
{
    // Arrange
    _userRepoMock.Setup(r => r.GetByEmail(It.IsAny<string>()))
        .Returns(Maybe<User>.Some(new User()));

    // Act
    var result = _sut.CreateUser(new CreateUserParameters { Email = "x@y.com" });

    // Assert
    Assert.True(result.Failed);
    Assert.Equal(OperationFailure.Conflict, result.Error.FailureType);
}
```

Key patterns:
- Inject all dependencies via constructor mocks.
- Use `AutoFixture` where available for test data generation.
- Integration tests that need the database go in `Tests.Integration.Presentation.Web` and must
  be in `[Collection(nameof(SequentialTestGroup))]` — they run sequentially.

Build and test commands:
```powershell
dotnet build KITOS.sln
dotnet test Tests.Unit.Core.ApplicationServices
dotnet test Tests.Unit.Presentation.Web
# Run a single test class
dotnet test Tests.Unit.Core.ApplicationServices --filter "FullyQualifiedName~ItSystemWriteServiceTest"
```

---

## DI Registration

New services are registered in `Presentation.Web/Infrastructure/DI/KitosServiceRegistration.cs`.

- Use `services.AddScoped<>` for request-scoped services (most services).
- Use `services.AddSingleton<>` for stateless utilities.
- Factory lambdas `sp => new MyService(sp.GetRequiredService<IDep>(), ...)` are used for
  services that need runtime configuration values.

---

## Key Conventions Checklist

- [ ] No business exceptions — use `Result`/`Maybe` monads.
- [ ] No direct `KitosContext` access in application or domain layers.
- [ ] Domain mutations through domain methods, not property setters.
- [ ] Every schema change has an EF6 migration.
- [ ] New endpoints have paired unit tests.
- [ ] Mappers registered in `KitosServiceRegistration.cs`.
- [ ] `From` address set (or rely on `DefaultFromAddressMailClient` decorator) for any new
  `MailMessage` usage.
