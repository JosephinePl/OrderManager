# ADR-004 — Domain Events: Decision Deferred

**Status:** Deferred  
**Date:** 2026-11-01

---

## Context

Domain events are a candidate pattern for decoupling side-effects from service methods. The most natural showcase use case is the production line export:

```
order.Complete()
  → raises OrderReadyForProductionEvent
  → ExportOrderOnReadyHandler calls IProductionLineExporter.ExportAsync()
```

In-process domain events (via MediatR or a simple hand-rolled dispatcher) can be promoted to a message broker (RabbitMQ, Azure Service Bus) by swapping only the `IDomainEventDispatcher` adapter — handler and service code stay unchanged.

## Arguments for

- Open/Closed Principle: adding a new side-effect (e.g. Slack alert on order creation) means a new handler — zero changes to `OrderService`.
- Explicit side-effects: each consequence is a named class; the handler list is authoritative documentation.
- Natural stepping stone: in-process events → message broker is a single adapter swap.
- Audit log for free via a generic `DomainEventLogger` handler.

## Arguments against

- Ceremony for a 3-entity CRUD app; meaningful boilerplate when `OrderService.Create()` has three lines.
- Debugging is indirect — a call stack fanning out to N handlers is harder to follow.
- Transaction boundary risk: dispatching before `SaveChanges()` means handlers read stale state; dispatching after means a handler failure cannot roll back a committed write.
- Test complexity: unit tests must assert that the correct event was raised, or mock the dispatcher and miss handler wiring.

## Decision

**Deferred.** Domain events are a strong talking point for the coding challenge interview, specifically the `OrderReadyForProductionEvent` → `ExportOrderOnReadyHandler` path. Whether to include them — and to what depth — will be decided when concrete implementation begins.

The key question to resolve at that point: **dispatch before or after `SaveChanges()`**, and which failure mode is acceptable.

## Implementation sketch (if adopted)

See the "Domain Events in Option B" section of [architecture-concept.md](../architecture-concept.md) for the full code outline (`IDomainEvent`, `IDomainEventDispatcher`, entity event collection, MediatR adapter).
