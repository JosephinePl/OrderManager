# Option B — REST API + Blazor WASM (Hexagonal / Ports & Adapters)

**This is the recommended option.** See [../architecture-concept.md](../architecture-concept.md) for the full rationale.

Two separate deployable units:
- `OrderManager.Api` — ASP.NET Core REST API, structured with hexagonal (Ports & Adapters) architecture
- `OrderManager.Blazor` — Blazor WebAssembly HMI, runs entirely in the browser after first load

The hexagonal core (`Core` + `Business`) defines interfaces ("ports"). HTTP controllers and EF Core repositories are "adapters" that plug in from outside. The domain never references any infrastructure package.

```mermaid
graph TD
    Browser["Browser"]
    PROD["Production Line\n(external consumer)"]

    subgraph HMI["HMI Service — OrderManager.Blazor (WebAssembly in browser)"]
        BPAGES["Blazor Pages &\nComponents\n(Orders, Boards, Components)"]
        BCLIENT["OrderManagerApiClient\n(typed HttpClient)"]
    end

    subgraph API["Business Service — OrderManager.Api (ASP.NET Core)"]
        CTRL["REST Controllers / Minimal API\nGET POST PUT DELETE\n[HTTP driving adapter]"]

        subgraph HEX["Hexagonal Core"]
            direction TB
            APPCORE["Application Services\nOrderManager.Business\n(pure C# — no infrastructure references)"]
            IPORT["Repository Ports\nOrderManager.Core\nIOrderRepository · IBoardRepository · IComponentRepository"]
            DOM["Domain Models\nOrderManager.Core\nOrder · Board · Component"]
        end

        REPO["EF Core Repositories\nOrderManager.Data\n[persistence driven adapter]"]
        CTX["AppDbContext\nOrderManager.Data"]
    end

    DB[(SQLite\norder-manager.db)]

    Browser -->|"loads app (HTTPS)"| HMI
    BPAGES --> BCLIENT
    BCLIENT -->|"HTTPS / REST + JSON\n(CORS configured)"| CTRL
    PROD    -->|"HTTPS / REST + JSON\nGET /api/orders/{id}/export"| CTRL

    CTRL -->|"calls"| APPCORE
    APPCORE --> DOM
    APPCORE -->|"calls via port"| IPORT
    IPORT -.->|"implemented by"| REPO
    REPO --> CTX
    CTX -->|"EF Core"| DB
```

## Project references

```
OrderManager.Blazor   → (HttpClient only — no project references to server-side assemblies)
OrderManager.Api      → Business, Data, Core
OrderManager.Business → Core
OrderManager.Data     → Core
OrderManager.Core     → (none)
```

The dependency rule is enforced at compile time: if any developer adds `OrderManager.Data` as a reference to `OrderManager.Business`, the build fails.

## Docker Compose topology

```
┌─────────────────────────────────┐
│  docker-compose.yml             │
│                                 │
│  blazor   (nginx:alpine)        │  ← serves WASM static files
│  api      (mcr.microsoft.com/   │  ← ASP.NET Core API
│             dotnet/aspnet:8.0)  │
│  (SQLite file via bind mount)   │
└─────────────────────────────────┘
```

## JSON export endpoint

`GET /api/orders/{id}/export` returns the order serialised to JSON using `System.Text.Json`. The production line calls this endpoint directly. The Blazor UI can offer a download button that navigates to the same URL.
