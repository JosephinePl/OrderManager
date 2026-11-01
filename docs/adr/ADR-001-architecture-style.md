# ADR-001 — Architecture Style: Hexagonal REST API (Core Deliverable) + Optional Blazor WASM

**Status:** Accepted  
**Date:** 2026-11-01

---

## Context

The coding challenge has two tiers of requirements:

**Mandatory:** CRUD management of Orders, Boards, and Components with JSON export to an SMT production line.

**Optional (bonus):** Web interface communicating with the backend API, CRUD via UI, **authentication required** (custom login or identity provider such as Azure AD B2C / Firebase). Also optional: Dockerisation, CI/CD, cloud deployment.

The company's strategic direction is microservices on Kubernetes. The chosen architecture must work for the challenge deliverable today and must not require a full rewrite to reach that future state.

Five options were evaluated (see [architecture-concept.md](../architecture-concept.md)):

| Option | Pattern | REST API | Microservices path |
|---|---|---|---|
| A | Blazor Server monolith | No | Full rewrite needed |
| **B** | **Hexagonal modular monolith** | **Yes** | **Additive** |
| C | REST API + MAUI | Yes | Additive |
| D | Vertical Slice + Blazor WASM | Yes | Additive |
| E | Clean Architecture + Blazor Server | No | Full rewrite needed |

## Decision

**Option B — Hexagonal (Ports & Adapters) REST API as the core deliverable.**

One deployable unit (single Docker image). Clean internal module boundaries enforced at the MSBuild level via `.csproj` references. Scalar UI (`Scalar.AspNetCore`) serves as the interactive interface for the mandatory deliverable — no frontend project required.

`OrderManager.Blazor` is **phase-2 / optional**. It is only added if time permits and only alongside an authentication layer (the challenge spec requires login if a web UI is included). Blazor WASM would be served as static files from the API host — no separate container, no CORS.

### Core project structure (mandatory)

```
src/
  OrderManager.Core/        # Ports: domain models + interfaces (no outbound deps)
  OrderManager.Business/    # Application core: services (depends only on Core)
  OrderManager.Data/        # Driven adapter: EF Core repositories (depends on Core)
  OrderManager.Api/         # Driving adapter: ASP.NET Core REST API + Scalar UI
tests/
  OrderManager.Tests.Unit/  # xUnit; EF Core in-memory provider — no real DB
```

### Optional phase-2 addition

```
src/
  OrderManager.Blazor/      # HMI driving adapter: Blazor WASM (HttpClient only)
                            # Requires authentication before this project is created
```

### Enforced dependency rule

```
Blazor (if added)  →  (HttpClient only — no server project references)
Api                →  Business, Data, Core
Business           →  Core
Data               →  Core
Core               →  (no references)
```

A developer adding `OrderManager.Data` as a reference to `OrderManager.Business` causes a build failure — architecture violations are compile errors.

### Phased delivery

| Phase | Scope |
|---|---|
| 1 | `Core + Business + Data + Api` — REST CRUD endpoints + Scalar UI |
| 2 | `Dockerfile` + `docker-compose.yml` — Api + PostgreSQL |
| 3 | GitHub Actions CI/CD — build + test on every commit |
| 4 (bonus) | `Blazor` WASM frontend with authentication |
| 5 (bonus) | Cloud deployment (Azure App Service / AKS) |

## Consequences

- REST endpoint is the natural delivery mechanism for the JSON export requirement — no UI workaround needed.
- Domain (`Business` + `Core`) has zero infrastructure dependencies — fully unit-testable without EF Core or ASP.NET.
- Scalar UI satisfies all mandatory CRUD interaction without a frontend project.
- Blazor WASM is only meaningful with authentication; adding it without auth would not satisfy the optional requirement.
- The API is already consumable by other services and future microservices — the hexagonal structure makes splitting additive, not a rewrite.
