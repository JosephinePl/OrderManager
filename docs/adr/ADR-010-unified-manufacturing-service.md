# ADR-010 — Consolidate Three Entity Services into One `IManufacturingService`

**Status:** Accepted  
**Date:** 2026-11-02  
**Supersedes:** Implicit one-service-per-entity convention established alongside [ADR-009](ADR-009-split-data-ports.md)

---

## Context

The initial design paired each entity with its own service interface and implementation:

```
IOrderService     →  OrderService
IBoardService     →  BoardService
IComponentService →  ComponentService
```

The domain has three independent aggregates connected by two many-to-many joins:

- An Order can include one or more Boards, and a Board can appear in multiple Orders (`OrderBoard` join table).
- A Board can contain one or more Components, and a Component can be placed on multiple Boards (`BoardComponent` join table).

Each entity has its own independent lifecycle: a Board exists before it is linked to any Order; a Component exists before it is placed on any Board. Deleting an Order removes its `OrderBoard` rows but leaves the Boards untouched; the same pattern applies to Board/Component.

Two boundary violations emerged during implementation:

1. `OrderService` injected `IBoardDataPort` to verify that a board exists before calling `AddBoardToOrderAsync`. A service was reaching into another entity's data boundary — exactly what the service split was supposed to prevent.
2. `BoardService` injected `IComponentDataPort` for the same reason, one level down.

The root cause is that association operations (`AddBoardToOrder`, `AddComponentToBoard`) inherently span two independent aggregates — they need both entities' data ports simultaneously. No single-aggregate service can own those operations without importing another aggregate's data port, so the three-service split forced the boundary violations.

Controllers injecting three different services was a secondary symptom: any operation touching two aggregates would require a controller to coordinate between services — responsibility that belongs in the service layer.

---

## Decision

Replace the three service interfaces and their implementations with a single driven port and implementation:

```
IManufacturingService  (Core/Ports)
ManufacturingService   (Business/Services)
```

`ManufacturingService` receives all three data ports as first-class constructor dependencies — no service-to-service calls, no cross-boundary injection:

```
ManufacturingService → IOrderDataPort
                     → IBoardDataPort
                     → IComponentDataPort
                     → IProductionLineExporter
```

All three controllers now inject `IManufacturingService`. The HTTP routes, status codes, and request/response shapes are unchanged.

---

## Why the data ports stay split

ADR-009 split `IOrderManagerDataPort` into three entity-specific ports to comply with the Interface Segregation Principle at the persistence layer. That reasoning is unaffected:

- Each port is a narrow, focused persistence contract.
- Unit tests for a single data-layer concern can substitute one small interface instead of the full fifteen-method surface.
- Swapping one backend (e.g. sourcing components from an external inventory API) touches only `IComponentDataPort`.

The asymmetry is intentional:

```
One service             Three data ports
──────────────          ──────────────────────────
IManufacturingService → IOrderDataPort
                      → IBoardDataPort
                      → IComponentDataPort
```

ISP applies at the data layer because the ports model independent persistence concerns. At the service layer, the domain cross-cutting is the feature, not a violation.

---

## Consequences

- **No cross-boundary injections:** `ManufacturingService` has direct access to all three data ports; no service injects another service's data port.
- **Single DI registration:** `Program.cs` registers `IManufacturingService → ManufacturingService` once instead of three separate service registrations.
- **No API surface change:** controllers keep their routes; only the injected type changes from a per-entity service to the unified service.
- **Wider mock surface per test class:** unit tests for `ManufacturingService` must substitute all three data ports. The individual mocking surface per operation is unchanged — each test only sets up the ports it exercises.
- **Linear growth:** adding a new entity still requires adding one data port interface, implementing it in `EfCoreDataAdapter`, and adding its methods to `IManufacturingService` and `ManufacturingService`.
