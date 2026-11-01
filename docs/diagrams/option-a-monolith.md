# Option A — Blazor Server Monolith (Layered)

A single ASP.NET Core process hosts both the Blazor Server UI and all business logic. UI events travel over a persistent SignalR WebSocket; no HTTP boundary exists between the UI and the domain.

```mermaid
graph TD
    Browser["Browser"]

    subgraph Host["ASP.NET Core Host — OrderManager.Web"]
        UI["Blazor Server\n(Razor Components, Pages)"]
        SVC["Application Services\nOrderManager.Business"]
        REPO["EF Core Repositories\nOrderManager.Data"]
        CTX["AppDbContext\nOrderManager.Data"]
    end

    subgraph Core["OrderManager.Core (shared contracts — no external dependencies)"]
        DOM["Domain Models\nOrder · Board · Component"]
        IFACE["Repository Interfaces\nIOrderRepository · IBoardRepository · IComponentRepository"]
        SFACE["Service Interfaces\nIOrderService · IBoardService · IComponentService"]
    end

    DB[(SQLite\norder-manager.db)]

    Browser -->|"SignalR / WebSocket"| UI
    UI -->|"DI"| SVC
    SVC -->|"DI"| REPO
    REPO --> CTX
    CTX -->|"EF Core"| DB

    SVC -.->|"implements"| SFACE
    REPO -.->|"implements"| IFACE
    SVC --> DOM
    REPO --> DOM
```

## Project references

```
OrderManager.Web      → Business, Data, Core
OrderManager.Business → Core
OrderManager.Data     → Core
OrderManager.Core     → (none)
```

## Key trade-off

No REST API means JSON export to the production line must be a file-download action on a Blazor page (`NavigationManager.NavigateTo("/api/orders/{id}/export")`), rather than a first-class REST endpoint. Workable, but less clean and not reusable by external consumers.
