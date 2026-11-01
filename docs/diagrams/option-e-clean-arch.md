# Option E — Clean Architecture + Blazor Server

Robert C. Martin's Clean Architecture applied as concentric rings. Inner rings have no dependency on outer rings — enforced at the `.csproj` reference level. Blazor Server is the outer ring: a single deployable, no REST API needed.

```mermaid
graph TD
    Browser["Browser"]

    subgraph WEB["OrderManager.Web — Blazor Server (outer ring)"]
        UI["Razor Components / Pages\n(Orders, Boards, Components)"]
        DI["DI Composition Root\nProgram.cs\n(wires Application ↔ Infrastructure)"]
    end

    subgraph INFRA["OrderManager.Infrastructure (framework adapter ring)"]
        REPO["EF Core Repositories\nOrderRepository · BoardRepository · ComponentRepository\nimplements interfaces from Application ring"]
        CTX["AppDbContext\n(EF Core — SQLite)"]
        LOG["Serilog Sink\n(file + console)"]
    end

    subgraph APP["OrderManager.Application (use-case ring)"]
        USECASES["Use Cases\nCreateOrderUseCase\nAssignBoardToOrderUseCase\nExportOrderUseCase\nDeleteOrderUseCase …"]
        IFACES["Repository Interfaces\nIOrderRepository\nIBoardRepository\nIComponentRepository"]
        DTOS["Request / Response DTOs\n(no domain objects cross the boundary)"]
    end

    subgraph DOM["OrderManager.Domain (innermost ring — zero external dependencies)"]
        ENT["Entities\nOrder · Board · Component"]
        VOs["Value Objects\nOrderDate"]
        RULES["Domain Rules\n(validation logic on entities)"]
    end

    DB[(SQLite\norder-manager.db)]

    Browser -->|"SignalR / WebSocket"| UI
    UI -->|"DI — calls use cases"| USECASES
    DI -->|"registers Infrastructure impls"| REPO
    DI -->|"registers use cases"| USECASES

    USECASES --> IFACES
    USECASES --> ENT
    USECASES --> DTOS
    IFACES -.->|"implemented by"| REPO
    REPO --> CTX
    CTX -->|"EF Core"| DB
    LOG -.->|"used by"| USECASES
```

## Project references (the dependency rule)

```
OrderManager.Web            → Application, Infrastructure (only for DI registration)
OrderManager.Infrastructure → Application, Domain
OrderManager.Application    → Domain
OrderManager.Domain         → (none)
```

`OrderManager.Web` references `Infrastructure` only in `Program.cs` for DI registration. All other `Web` code calls only `Application` interfaces — it never touches `Infrastructure` types directly.

## Key trade-off

The strictest dependency inversion of all options, but also the most boilerplate for a 3-entity CRUD app. Like Option A, it has no REST API, which means the JSON export to the production line requires a workaround rather than a clean endpoint. Also the same Blazor Server SignalR concern applies: horizontal scaling requires sticky sessions.
