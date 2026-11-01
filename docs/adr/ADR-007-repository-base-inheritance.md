# ADR-007 — Repository Implementation: Inheritance over Composition for `RepositoryBase<T>`

**Status:** Accepted  
**Date:** 2026-11-01

---

## Context

The Data adapter layer contains three EF Core repository implementations (`OrderRepository`, `BoardRepository`, `ComponentRepository`). All three perform identical CRUD operations via `DbContext.Set<T>()`. The question is how to share that implementation without duplicating it.

Two approaches were evaluated:

**A — Inheritance via `RepositoryBase<T>`** (chosen)

```
RepositoryBase<T>  :  IRepository<T>    ← EF Core CRUD implementation
    └── OrderRepository  :  IOrderRepository
    └── BoardRepository  :  IBoardRepository
    └── ComponentRepository  :  IComponentRepository
```

**B — Composition via a generic `EfRepository<T>`**

```
EfRepository<T>  :  IRepository<T>      ← EF Core CRUD implementation (registered via open generics)
    ← injected into →
OrderRepository  :  IOrderRepository    ← delegates CRUD, adds entity-specific methods
```

"Favour composition over inheritance" is a well-established principle. Option B was considered seriously.

## Why Composition Was Rejected

### C# Has No Delegation Syntax

Languages such as Kotlin provide a `by` keyword that generates forwarding boilerplate at compile time:

```kotlin
class OrderRepository(inner: IRepository<Order>) : IOrderRepository by inner
```

C# has no equivalent. Every method in `IRepository<T>` must be manually delegated in every entity repository:

```csharp
public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) => _inner.GetByIdAsync(id, ct);
public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct) => _inner.GetAllAsync(ct);
// ... three more per repository, nine total across three repositories
```

The mechanism that would make composition appealing simply does not exist in C#.

### The Automated Alternatives Are Disproportionate

- **`DispatchProxy`** — generates a dynamic proxy at runtime that intercepts interface calls. Loses compile-time type safety, adds reflection overhead, and makes debugging significantly harder.
- **Source generators** — generate the delegation boilerplate at compile time. Correct approach for a reusable framework; unjustifiable for three repository classes in a single application.

### The `is-a` Relationship Is Semantically Correct

"Favour composition over inheritance" targets **behavioural inheritance** — inheriting from a type to reuse behaviour when the `is-a` relationship is conceptually wrong (the canonical anti-example: `Stack extends Vector`).

`OrderRepository` IS a repository for `Order`. The inheritance is not coupling unrelated concepts; it is sharing identical infrastructure code within the same layer, the same technology, and the same abstraction level. This is the use case that abstract base classes are designed for.

### Decorator Remains the Pattern for Cross-Cutting Concerns

The composition principle applies when adding behaviour to an existing object from outside. That role is filled by the **Decorator pattern** (see [ADR-005](ADR-005-repository-caching.md)) for future caching:

```
CachingOrderRepository  (decorator — wraps from outside)
    └── OrderRepository (EF Core implementation)
```

Inheritance handles implementation sharing; Decorator handles behavioural extension. Both patterns coexist without conflict.

## Decision

**Option A — `RepositoryBase<T>` with inheritance.**

The entity repositories (`OrderRepository`, `BoardRepository`, `ComponentRepository`) extend `RepositoryBase<T>`, which holds the single EF Core CRUD implementation. Entity-specific query methods are added as overrides or new methods on the derived classes.

## Consequences

- Zero CRUD duplication across the three repositories.
- Entity-specific methods (e.g. `GetOrdersWithBoardsAsync`) are added to the derived class; the base is not touched.
- If a future entity needs different CRUD semantics it can override the relevant method.
- When the Decorator caching layer is added (ADR-005), it wraps the concrete repository from outside — the inheritance hierarchy is invisible to the decorator.
- The base class is internal to `OrderManager.Data`; `Core` never references it. The hexagonal dependency rule is not affected.
