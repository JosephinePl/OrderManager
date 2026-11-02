# ADR-009 — Split `IOrderManagerDataPort` into Three Entity-Specific Data Ports

**Status:** Accepted  
**Date:** 2026-11-02  
**Supersedes:** [ADR-008](ADR-008-unified-data-port.md)

---

## Context

ADR-008 replaced three per-entity repository interfaces with a single `IOrderManagerDataPort` to eliminate empty boilerplate classes. That decision was correct at the time: the repositories were empty subclasses with no entity-specific logic, and collapsing them into one interface removed eight files of structural noise.

The single port was then injected into all three service classes — `OrderService`, `BoardService`, and `ComponentService`. This violates the Interface Segregation Principle: `BoardService` carries a compile-time dependency on order and component persistence methods it never calls, and `ComponentService` does the same for order and board methods.

The violation became visible when comparing the service constructors: each service accepted the full `IOrderManagerDataPort` but only used its own entity's five methods. Mocking the full interface in tests requires stubbing methods that are irrelevant to the service under test.

---

## Decision

Replace `IOrderManagerDataPort` with three focused driven ports, one per entity:

```
IOrderDataPort      ← order CRUD + GetOrderWithDetailsAsync (Core/Ports)
IBoardDataPort      ← board CRUD (Core/Ports)
IComponentDataPort  ← component CRUD (Core/Ports)

EfCoreDataAdapter   ← implements all three (Data/Adapters)
```

Each service injects only its own port:

```
OrderService     → IOrderDataPort, IProductionLineExporter
BoardService     → IBoardDataPort
ComponentService → IComponentDataPort
```

`EfCoreDataAdapter` continues to be a single sealed class implementing all three interfaces. The EF Core `DbContext` is shared — splitting the adapter class would require either sharing context across instances or registering multiple scoped adapters against the same context, neither of which adds value.

---

## Consequences

- **ISP compliance:** Each service depends only on the methods it actually calls. `BoardService` no longer has a dependency on `GetOrderWithDetailsAsync`.
- **Clearer mocking surface:** Unit tests for `BoardService` substitute `IBoardDataPort` — a five-method interface — rather than the full fifteen-method interface.
- **Three DI registrations:** `Program.cs` registers `EfCoreDataAdapter` once per port interface. This is slightly more verbose but explicit about what the adapter provides.
- **ADR-008 rationale still holds for the adapter:** The single `EfCoreDataAdapter` class is retained. The per-entity split applies only to the port interfaces, not the implementation.
- **Linear growth unchanged:** Adding a new entity still means adding one interface file and implementing its methods in `EfCoreDataAdapter`.
