# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OrderManager is a C# (.NET) application for managing SMT (Surface-Mount Technology) manufacturing orders. It is built from a coding challenge specification located at [docs/story/ASMPT_Coding_Challenge_EN.pdf](docs/story/ASMPT_Coding_Challenge_EN.pdf).

## Build, Test, and Run Commands

Once the solution is set up, these are the standard commands:

```powershell
dotnet build                          # Build the solution
dotnet test                           # Run all tests
dotnet test --filter "FullyQualifiedName~ClassName" # Run a single test class
dotnet run --project src/<ProjectName> # Run the application
```

## Domain Model

Three core entities with the following relationships:

- **Order** — Id (Guid), Name, Description, OrderDate — has many **Boards** (many-to-many via `OrderBoard`)
- **Board** — Id (Guid), Name, Description, Length, Width — belongs to many **Orders**, has many **Components** (many-to-many via `BoardComponent`)
- **Component** — Id (Guid), Name, Description, StockQuantity — belongs to many **Boards**

## Architecture

Decided: **Hexagonal (Ports & Adapters) REST API** — see [docs/adr/ADR-001-architecture-style.md](docs/adr/ADR-001-architecture-style.md).

The web UI (`OrderManager.Blazor`) is **optional / phase-2** and only added alongside authentication — it is not part of the core deliverable.

```
src/
  OrderManager.Core/        # Ports: domain models + interfaces (zero outbound deps)
  OrderManager.Business/    # Application core: services (depends only on Core)
  OrderManager.Data/        # Driven adapter: EF Core repositories (depends on Core)
  OrderManager.Api/         # Driving adapter: ASP.NET Core REST API + Scalar UI
tests/
  OrderManager.Tests.Unit/  # xUnit; EF Core in-memory provider — no real DB
```

### Enforced dependency rule

```
Api       →  Business, Data, Core
Business  →  Core
Data      →  Core
Core      →  (no references)
```

Violations are compile errors — `OrderManager.Data` must never be referenced by `OrderManager.Business`.

### Key design requirements

- **Repository pattern**: interfaces in `Core`, implementations in `Data`
- **Production line export**: modelled as `IProductionLineExporter` port in `Core`; `SimulatedFileExportAdapter` registered for Development (see [ADR-003](docs/adr/ADR-003-production-line-export.md))
- **Dependency injection** via `Microsoft.Extensions.DependencyInjection`
- **Logging** via `Microsoft.Extensions.Logging` or Serilog
- **JSON serialization** via `System.Text.Json` (no third-party serializer)
- At least one unit test covering a service method
- **Authentication is required** if the optional Blazor web UI is added (challenge spec mandates login for any web interface)

## Architecture Decision Records

| ADR | Decision | Status |
|---|---|---|
| [ADR-001](docs/adr/ADR-001-architecture-style.md) | Hexagonal modular monolith (Option B) | Accepted |
| [ADR-002](docs/adr/ADR-002-database.md) | PostgreSQL (SQLite for local dev only) | Accepted |
| [ADR-003](docs/adr/ADR-003-production-line-export.md) | `IProductionLineExporter` named port | Accepted |
| [ADR-004](docs/adr/ADR-004-domain-events.md) | Domain events | Deferred |
