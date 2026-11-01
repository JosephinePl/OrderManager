# ADR-005 — Repository Caching: Decorator Pattern Deferred

**Status:** Deferred  
**Date:** 2026-11-01

---

## Context

The hexagonal architecture defines repository ports in `Core` (`IOrderRepository`, `IBoardRepository`, `IComponentRepository`) with EF Core implementations in `Data`. Because consumers depend only on the interface, a caching layer could be inserted transparently using the **Decorator pattern** — wrapping the real repository with a cached version registered under the same port:

```
IOrderRepository
  └── CachingOrderRepository   (checks cache, falls through on miss)
        └── OrderRepository    (EF Core — hits the database)
```

`Program.cs` would register the chain; no business or domain class would change. The architecture supports this for free.

## Decision

**Do not implement repository caching at this stage.**

## Rationale

| Reason | Detail |
|---|---|
| **Write-heavy domain** | Orders, Boards, and Components are actively created, updated, and deleted. A cache that must be invalidated on every mutation provides little read benefit and introduces correctness risk (stale reads after a write). |
| **No measured problem** | Caching is an optimisation for a known bottleneck. No load data exists for this application; adding a cache layer would be solving a hypothetical problem. |
| **Scope** | This is a coding-challenge deliverable. The added complexity of cache invalidation strategy, TTL tuning, and distributed-cache considerations (relevant once the API runs as multiple Kubernetes pods) is out of scope. |

## Where this would be valid

If `Component.StockQuantity` were read by many concurrent requests but updated infrequently (e.g. a product catalogue), a `CachingComponentRepository` with a short TTL would be a justified, clean application of the pattern. The port-based design makes that addition purely additive — zero changes to `ComponentService` or any other class.

## Consequences

- No caching infrastructure is added now.
- The Decorator option remains open: adding it later requires only a new wrapper class and a one-line DI registration change in `Program.cs`.
- If the API scales to multiple pods, any future cache implementation must be distributed (e.g. Redis via `IDistributedCache`) — an in-memory decorator would produce inconsistent reads across replicas.
