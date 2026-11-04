# ADR-011 — Domain Ownership: Boards and Components are Stammdaten

**Status:** Accepted
**Date:** 2026-11-04

---

## Context

The initial domain model gave `Board` back-navigation properties to its orders (`Board.OrderBoards`, `AddOrder`, `RemoveOrder`) and `Component` back-navigation properties to its boards (`Component.BoardComponents`, `AddBoard`, `RemoveBoard`). These existed to satisfy EF Core's relationship mapping on both sides of the many-to-many joins.

This caused two concrete problems:

1. **Wrong domain semantics**: the spec says *"An Order can include one or more Boards"* — not the reverse. `Board` knowing about its orders is a persistence query concern, not a domain concern.
2. **JSON cycle**: serialising an `Order` → `OrderBoards` → `OrderBoard.Board` → `Board.OrderBoards` → `OrderBoard.Order` created a circular reference. The workaround was `ReferenceHandler.IgnoreCycles`, which silently drops data on repeated visits — a correctness hazard.

---

## Decision

**Boards and Components are Stammdaten** — master data that exists independently of Orders.

- Navigation is **one-directional**: `Order → OrderBoard → Board`, `Board → BoardComponent → Component`.
- `Board` has no knowledge of which Orders it appears in.
- `Component` has no knowledge of which Boards it appears on.
- The join entities (`OrderBoard`, `BoardComponent`) carry a navigation property only in the ownership direction (`OrderBoard.Board`, `BoardComponent.Component`). The reverse properties (`OrderBoard.Order`, `BoardComponent.Board`) are removed.
- EF Core is configured with one-sided navigations using `WithMany()` on the owning side and no corresponding property on the other side.

**Cascade delete behaviour** (unchanged from initial schema):

- Deleting an `Order` cascade-deletes its `OrderBoard` junction rows only — `Board` and `Component` records are never touched.
- Deleting a `Board` cascade-deletes its `BoardComponent` junction rows only — `Component` records are never touched.
- Application-level guards (`IsBoardAssignedToAnyOrderAsync`, `IsComponentAssignedToAnyBoardAsync`) additionally prevent deletion of Stammdaten that are still referenced, returning a meaningful error rather than relying solely on a DB constraint.

**API response layer**: because domain entities are no longer serialised directly, **response DTOs** (`OrderResponse`, `BoardResponse`, and their nested summary types) are introduced in `OrderManager.Api/Models/`. Controllers map entities to DTOs before returning `Ok(...)`. This eliminates `ReferenceHandler.IgnoreCycles` and `[JsonPropertyName]` attributes from Core entities.

---

## Consequences

- `Board` and `Component` entities are simpler — no mutation methods for associations they don't own.
- No circular reference risk in serialisation.
- A thin DTO layer is added in the API project; it is the only place that shapes the HTTP response.
- EF Core still generates correct SQL for all required queries via one-sided navigation configuration.
- The database schema is **unchanged** — no new migration is required.
