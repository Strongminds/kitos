---
name: kitos-repositories
description: Create or refactor repositories in the KITOS codebase, reusing its generic repository for supported queries and persistence operations. Use when adding a repository or reviewing repository conventions in KITOS.
---

# KITOS repositories

Use `IGenericRepository<T>` or `GenericRepository<T>` whenever possible. Before adding a custom repository, check whether injecting `IGenericRepository<T>` directly into the consumer already satisfies the requirement.

## Choose the repository shape

- For domain-specific queries or orchestration, prefer a small repository that receives `IGenericRepository<TEntity>` through constructor injection. Build queries from `AsQueryable()` and delegate supported persistence operations to the generic repository.
- Inherit from `GenericRepository<TEntity>` when the specialized repository should expose the generic repository contract. Its interface can extend `IGenericRepository<TEntity>`. Reuse inherited methods such as `Insert()` and `Save()` instead of duplicating them with `DbSet.Add()` or `SaveChanges()`.
- Keep direct `KitosContext` access only for behavior the generic API cannot provide, such as explicit transactions, change-tracker operations, provider-specific SQL, or required asynchronous operations. Keep EF-specific implementation in `Infrastructure.DataAccess`.
- The KITOS generic implementation depends on `KitosContext`. Do not use it for entities in a separate context such as `PubSubContext`.

## Preserve behavior

Keep organization filters, eager loading, tracking choices, ordering, pagination, and save boundaries intact. Use `AsNoTracking()` for detached reads where appropriate; keep entities tracked when callers update them. Do not materialize a query before filtering and paging merely to use a generic helper.

`Insert()`, `Update()`, and deletion methods do not save automatically; call `Save()` only at the intended persistence boundary. Preserve duplicate-insert handling, entity detachment after failed writes, and concurrency handling when refactoring specialized persistence.

For deletion, inspect the generic repository's reference-preloading helpers and interface documentation: KITOS uses these to handle relationships that otherwise may not be cleaned up correctly.

## Follow the codebase

Resolve these paths relative to the KITOS repository root and inspect the current implementations before choosing methods:

- `Core.DomainServices/IGenericRepository.cs`: supported operations and deletion semantics.
- `Infrastructure.DataAccess/GenericRepository.cs`: implementation and context ownership.
- `Core.DomainServices/Repositories/Contract/ItContractSupplierOverviewReadModelRepository.cs`: composition with an injected generic repository.
- `Infrastructure.DataAccess/UserRepository.cs`: specialized repository inheritance.
- `Presentation.Web/Infrastructure/DI/KitosServiceRegistration.cs`: open generic registration and scoped specialized registrations.

Place repository contracts and domain-level implementations alongside related code in `Core.DomainServices`; place EF-dependent implementations in `Infrastructure.DataAccess`. Reuse the existing open generic DI registration and register any specialized interface with the appropriate scoped implementation. Do not dispose an injected repository inside a method: the generic implementation disposes its shared context.

Build affected projects and run relevant existing tests. Add focused behavior tests when introducing new filtering, persistence, or concurrency semantics. Report any direct context access retained and the concrete behavior that requires it.
