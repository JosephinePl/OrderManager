# Option C — REST API + MAUI Client (Hexagonal)

Same `OrderManager.Api` backend as Option B. The HMI is a .NET MAUI native application — a desktop/mobile shell that calls the API over HTTP and can additionally access native OS features (file system, COM/USB ports, OS notifications).

```mermaid
graph TD
    subgraph HMI["HMI Service — OrderManager.Maui (Native App — Windows / macOS / iOS / Android)"]
        MSHELL["MAUI Shell\n(AppShell, ContentPages)"]
        VM["ViewModels\n(CommunityToolkit.Mvvm)"]
        MCLIENT["OrderManagerApiClient\n(typed HttpClient)"]
        OS["Native OS Features\nFile I/O · COM/USB · Push Notifications"]
    end

    subgraph API["Business Service — OrderManager.Api (ASP.NET Core)"]
        CTRL["REST Controllers / Minimal API\n[HTTP driving adapter]"]

        subgraph HEX["Hexagonal Core"]
            direction TB
            APPCORE["Application Services\nOrderManager.Business\n(pure C# — no infrastructure references)"]
            IPORT["Repository Ports\nOrderManager.Core\nIOrderRepository · IBoardRepository · IComponentRepository"]
            DOM["Domain Models\nOrderManager.Core\nOrder · Board · Component"]
        end

        REPO["EF Core Repositories\nOrderManager.Data\n[persistence driven adapter]"]
        CTX["AppDbContext"]
    end

    DB[(SQLite\norder-manager.db)]

    MSHELL --> VM
    VM --> MCLIENT
    VM --> OS
    MCLIENT -->|"HTTPS / REST + JSON"| CTRL

    CTRL --> APPCORE
    APPCORE --> DOM
    APPCORE -->|"via port"| IPORT
    IPORT -.->|"implemented by"| REPO
    REPO --> CTX
    CTX -->|"EF Core"| DB
```

## Project references

```
OrderManager.Maui     → (HttpClient only — no project references to server-side assemblies)
OrderManager.Api      → Business, Data, Core
OrderManager.Business → Core
OrderManager.Data     → Core
OrderManager.Core     → (none)
```

## When to prefer this over Option B

- The HMI must run on factory-floor terminals without a browser
- The HMI needs direct access to local hardware (e.g. reading from SMT machine via COM port)
- Offline operation is required (MAUI can embed its own SQLite and sync later)

## Build complexity note

MAUI targets Android, iOS, macOS, and Windows from a single codebase. For a coding challenge targeting Windows only, set `<TargetFrameworks>net8.0-windows10.0.19041.0</TargetFrameworks>` in the `.csproj` to avoid requiring Android/iOS SDKs and a Mac agent.
