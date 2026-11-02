# ADR-008 — Unified Data Port: Replace Per-Entity Repositories with `IOrderManagerDataPort`

**Status:** Superseded by [ADR-009](ADR-009-split-data-ports.md)  
**Date:** 2026-11-02

---

## Context

The initial implementation used the Repository pattern with one interface and one implementation per entity:

```
IRepository<T>          ← generic port (Core)
  IOrderRepository      ← empty marker (Core)
  IBoardRepository      ← empty marker (Core)
  IComponentRepository  ← empty marker (Core)

RepositoryBase<T>       ← generic EF Core implementation (Data)
  OrderRepository       ← empty subclass (Data)
  BoardRepository       ← empty subclass (Data)
  ComponentRepository   ← empty subclass (Data)
```

All three concrete repositories were empty classes — they added no entity-specific logic whatsoever. The stated reason for their existence was that they were extension points for future entity-specific query methods. Those methods were never added, and the extension-point argument was premature optimism.

The result was 8 files of structural boilerplate that conveyed no information, imposed a per-entity file-creation tax for every new entity, and made ADR-007 necessary to justify the choice of inheritance over composition — a design discussion that existed only because of the per-entity split.

### Why the full Repository pattern was reconsidered

The Repository pattern is valuable when:
- Different entities have meaningfully different query APIs (e.g. `FindOrdersByCustomerId` vs `FindBoardsByDimensions`).
- The persistence technology is likely to change, and the per-entity interface isolates that risk.
- Teams want to mock individual repositories in unit tests.

None of these applied here at implementation time. The entities share identical CRUD APIs, EF Core is not being swapped out, and mocking a single `IOrderManagerDataPort` is no harder than mocking three individual repository interfaces.

---

## Decision

Replace all per-entity repository interfaces and implementations with a **single unified data port**:

```
IOrderManagerDataPort   ← one port contract (Core/Ports)
EfCoreDataAdapter       ← one EF Core implementation (Data/Adapters)
```

`IOrderManagerDataPort` exposes typed CRUD methods for each entity (e.g. `GetOrderByIdAsync`, `AddBoardAsync`). When a future entity requires a domain-specific query method, it is added to this one interface — no new file is needed.

`EfCoreDataAdapter` is a sealed class that injects `OrderManagerDbContext` and implements all methods directly. A private generic helper method avoids repeating the add/update/save pattern per entity type.

`IProductionLineExporter` remains a **separate port** — it models an integration concern (export to the factory floor), not a persistence concern. Collapsing it into the data port would conflate two different hexagonal boundaries.

---

## Consequences

- **Fewer files:** 8 boilerplate files (4 interfaces + 4 classes) become 2 files (1 interface + 1 class).
- **Linear growth:** Adding a new entity means adding 5 methods to `IOrderManagerDataPort` and 5 implementations to `EfCoreDataAdapter` — no new files.
- **Simpler DI:** One `services.AddScoped<IOrderManagerDataPort, EfCoreDataAdapter>()` registration replaces three.
- **Entity-specific queries:** If `Order` ever needs `GetOrdersWithBoardsAsync`, it is added to `IOrderManagerDataPort`. If the method only belongs to one context, a focused secondary port interface can be introduced at that point with clear justification — not as a speculative placeholder.
- **Testing:** Services mock `IOrderManagerDataPort` via NSubstitute. The in-memory EF Core integration tests move from `RepositoryBaseTests` to `EfCoreDataAdapterTests`.
- **ADR-007 superseded:** The inheritance-vs-composition discussion in ADR-007 no longer applies — there is no generic base class to inherit from.
