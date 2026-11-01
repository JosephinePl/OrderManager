# Option D — Vertical Slice Architecture + Blazor WASM

Instead of horizontal layers (Core / Business / Data / Api), the API is organised around **features** — vertical slices. Each feature folder is self-contained: the command/query class, the handler (business logic lives here), a validator, and the Minimal API endpoint registration. MediatR dispatches requests to handlers. EF Core `DbContext` is used directly inside handlers — no generic repository abstraction.

```mermaid
graph TD
    Browser["Browser"]

    subgraph HMI["HMI — OrderManager.Blazor (WebAssembly)"]
        BPAGES["Blazor Pages\n(Orders, Boards, Components)"]
        BCLIENT["ApiClient\n(typed HttpClient)"]
    end

    subgraph API["OrderManager.Api (ASP.NET Core)"]
        MED["MediatR\nDispatcher"]

        subgraph ORDERS["Orders Feature Slice\nFeatures/Orders/"]
            O_EP["POST /api/orders\nGET /api/orders\nGET /api/orders/{id}\nDELETE /api/orders/{id}\nGET /api/orders/{id}/export"]
            O_HDL["CreateOrderHandler\nGetOrdersHandler\nGetOrderByIdHandler\nDeleteOrderHandler\nExportOrderHandler\n(business logic lives here)"]
        end

        subgraph BOARDS["Boards Feature Slice\nFeatures/Boards/"]
            B_EP["POST /api/boards\nGET /api/boards\nGET /api/boards/{id}"]
            B_HDL["CreateBoardHandler\nGetBoardsHandler\nGetBoardByIdHandler"]
        end

        subgraph COMPS["Components Feature Slice\nFeatures/Components/"]
            C_EP["POST /api/components\nGET /api/components\nGET /api/components/{id}"]
            C_HDL["CreateComponentHandler\nGetComponentsHandler\nGetComponentByIdHandler"]
        end

        CTX["AppDbContext\nInfrastructure/Persistence/"]
    end

    DB[(SQLite\norder-manager.db)]

    Browser --> BPAGES
    BPAGES --> BCLIENT
    BCLIENT -->|"HTTPS / REST + JSON"| O_EP
    BCLIENT -->|"HTTPS / REST + JSON"| B_EP
    BCLIENT -->|"HTTPS / REST + JSON"| C_EP

    O_EP -->|"Send(command/query)"| MED
    B_EP -->|"Send(command/query)"| MED
    C_EP -->|"Send(command/query)"| MED

    MED --> O_HDL
    MED --> B_HDL
    MED --> C_HDL

    O_HDL -->|"EF Core directly"| CTX
    B_HDL -->|"EF Core directly"| CTX
    C_HDL -->|"EF Core directly"| CTX
    CTX -->|"EF Core"| DB
```

## Folder structure

```
src/OrderManager.Api/
  Features/
    Orders/
      CreateOrder/
        CreateOrderCommand.cs
        CreateOrderHandler.cs
        CreateOrderValidator.cs
      GetOrders/
        GetOrdersQuery.cs
        GetOrdersHandler.cs
      ExportOrder/
        ExportOrderQuery.cs
        ExportOrderHandler.cs
      OrdersModule.cs        ← registers all /api/orders/* endpoints
    Boards/
      CreateBoard/ …
      BoardsModule.cs
    Components/
      CreateComponent/ …
      ComponentsModule.cs
  Infrastructure/
    Persistence/
      AppDbContext.cs
      Migrations/
  Program.cs
src/OrderManager.Blazor/     ← same as Option B
```

## Key trade-off vs Option B

Option D skips the generic Repository layer — each handler uses `AppDbContext` directly. This removes boilerplate but means the challenge's explicit Repository pattern requirement is not demonstrated. Option B is better if the rubric awards points for Repository + interface separation.
