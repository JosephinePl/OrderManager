# OrderManager — Architecture Concept

## Introduction

This document compares candidate architectures for the OrderManager application, a C# (.NET) system for managing SMT (Surface-Mount Technology) manufacturing orders.

**Goal:** Choose an architecture that satisfies the coding challenge requirements today and is a credible stepping stone toward the company's microservices and Kubernetes roadmap.

**Constraints:**
- All technology must stay in the C# / .NET ecosystem
- Platform-independent (Windows, Linux, macOS, Docker)
- Core domain: CRUD for Orders, Boards, and Components with many-to-many relationships
- JSON export to the production line is a first-class requirement

**Company context:** The company currently runs legacy .NET Framework 4.x code and is actively moving toward microservices on Docker and Kubernetes. The chosen architecture must not require a full rewrite to reach that future state.

---

## Glossary

| Term | Meaning |
|---|---|
| **WASM / WebAssembly** | A binary format that runs in the browser. Blazor WASM compiles C# to WebAssembly so the UI runs client-side — no server needed after the first download. |
| **Hexagonal Architecture** | Also called Ports & Adapters. The application core (business logic + domain) defines interfaces ("ports"). External concerns — HTTP, database, UI — are "adapters" plugged into those ports. The core never depends on infrastructure; infrastructure depends on the core. |
| **CORS** | Cross-Origin Resource Sharing. A browser security rule: when Blazor WASM (e.g. `localhost:5001`) calls the API (e.g. `localhost:5002`), the browser blocks it unless the API explicitly allows it via a response header. Fixed with `app.UseCors()` in `Program.cs`. |
| **EF Core** | Entity Framework Core — the standard .NET ORM. Maps C# classes to database tables and handles SQL generation. |
| **MediatR** | A .NET library for the mediator pattern. Commands and queries are dispatched through it to handlers; used in Vertical Slice Architecture. |
| **SignalR** | Microsoft's real-time communication library. Blazor Server uses it to sync UI events between browser and server over a persistent WebSocket. |

---

## Domain Model

Three entities linked by two junction tables.

See [diagrams/domain-model.md](diagrams/domain-model.md) for the full ERD.

| Entity | Key Fields |
|---|---|
| Order | Id (Guid), Name, Description, OrderDate |
| Board | Id (Guid), Name, Description, Length (decimal), Width (decimal) |
| Component | Id (Guid), Name, Description, StockQuantity (int) |
| OrderBoard | OrderId (FK), BoardId (FK) |
| BoardComponent | BoardId (FK), ComponentId (FK), QuantityPerBoard (int) |

---

## Database

**Selected: PostgreSQL** (SQL Server is an equally valid alternative for teams with an existing MSSQL license)

The JSON export (sending an order to the production line) is performed in C# using `System.Text.Json` — the database only stores and queries rows. PostgreSQL is:
- Runs natively in a lightweight Linux container; integrates easily with Kubernetes Persistent Volume Claims
- Handles concurrent multi-replica writes without file-level locking
- Fully supported by EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- Available as a managed service on every major cloud (Azure Database for PostgreSQL, AWS RDS, GCP Cloud SQL)

**Why not SQLite?** SQLite uses a single local file with file-level locking. In Kubernetes, if the API scales to multiple pods or a pod is rescheduled to a different node, the file state is lost or locked — causing concurrency crashes. SQLite is acceptable for local developer-only runs (`appsettings.Development.json`) and EF Core in-memory provider for unit tests, but must not be the container image's runtime database.

Swapping providers is a one-line connection string change plus `dotnet ef migrations add` — the hexagonal adapter pattern ensures zero business logic changes.

---

## Architecture Options

### Option A — Blazor Server Monolith (Layered)

A single ASP.NET Core process hosts both the Blazor Server UI and all business logic. No HTTP boundary between UI and domain. UI events travel over a persistent SignalR WebSocket.

**Projects:** `Core / Business / Data / Web`

See [diagrams/option-a-monolith.md](diagrams/option-a-monolith.md)

**Pros:**
- Minimal moving parts — one process, one deploy artifact
- Fastest to scaffold; ideal for very tight deadlines
- No CORS, no REST surface area to design
- Single attach point for debugging

**Cons:**
- No REST API — JSON export to the production line requires a workaround (a download button on a Blazor page instead of a clean endpoint)
- SignalR requires a persistent connection per user; horizontal scaling needs sticky sessions
- UI and business logic in the same process — must be fully decomposed before it can run as independent Kubernetes deployments
- Dead end for the company's microservices roadmap

---

### Option B — Hexagonal REST API + Optional Blazor WASM ← Recommended

`OrderManager.Api` is a self-contained hexagonal service: one deployable unit, one Docker image, clean internal module boundaries. Scalar UI (`Scalar.AspNetCore`) serves as the interactive interface for all mandatory CRUD operations — no frontend project is required to satisfy the challenge.

`OrderManager.Blazor` (Blazor WASM) is an **optional addition** that satisfies the *Web Interface* optional requirement. The challenge spec mandates **authentication (login)** if a web UI is included — Blazor is therefore only added alongside an auth layer. When added, it is served as static files directly from the API host — no separate container, no CORS.

**What "hexagonal" means here:** The domain core (`Core` + `Business`) defines interfaces ("ports") for everything it needs — `IOrderDataPort`, `IBoardDataPort`, `IComponentDataPort`, `IOrderService`, `IProductionLineExporter`, etc. HTTP controllers, the EF Core data adapter, and the simulated production-line adapter are "adapters" plugged in from outside at startup. The domain never references any infrastructure package.

**Production line port:** The JSON export to the SMT line is modelled as a named port:

```csharp
// Core/Ports/IProductionLineExporter.cs
public interface IProductionLineExporter
{
    Task ExportAsync(Order order, CancellationToken ct = default);
}

// Data/Adapters/SimulatedFileExportAdapter.cs  (registered for Development)
// Infrastructure/Adapters/MqttProductionLineAdapter.cs  (registered for Production — future)
```

Swapping the simulation for a real factory-floor adapter (MQTT, IPC-CFX) is a single DI registration change — zero business logic changes.

**Core projects:** `Core / Business / Data / Api`  
**Optional:** `Blazor` (requires authentication — see optional requirements)

See [diagrams/option-b-api-blazor.md](diagrams/option-b-api-blazor.md)

**Pros:**
- REST endpoint is the natural delivery mechanism for the JSON export requirement
- Domain (`Business` + `Core`) has zero infrastructure dependencies — fully unit-testable without EF Core or ASP.NET
- Scalar UI satisfies all mandatory CRUD interaction without a frontend project
- API can be consumed by the production line, a future MAUI client, or any external system
- Already a clean stepping stone to microservices on Kubernetes — splitting is additive, not a rewrite
- If Blazor is added: served as static files from the API host via `UseBlazorFrameworkFiles()` + `UseStaticFiles()` — no separate nginx container, no CORS
- PostgreSQL runs as a sidecar container in Docker Compose / as a managed service in Kubernetes

**Cons:**
- More boilerplate than a plain monolith: DTOs, mapping, OpenAPI/Scalar setup
- If Blazor is added: two processes to start during development without the hosted-WASM launch profile; noticeable first-load time (downloads .NET runtime assemblies; cached thereafter)
- If Blazor is added: authentication (ASP.NET Core Identity, Azure AD B2C, or Firebase) is a mandatory companion — not optional within the web-UI scope

---

### Option C — REST API + MAUI Client (Hexagonal)

Same `OrderManager.Api` backend as Option B. The HMI is a .NET MAUI native application — a desktop/mobile shell that calls the API over HTTP and can additionally access native OS features.

**Projects:** `Core / Business / Data / Api / Maui`

See [diagrams/option-c-api-maui.md](diagrams/option-c-api-maui.md)

**Pros:**
- Native look-and-feel on Windows, macOS, iOS, Android from a single codebase
- Can access machine hardware directly (COM/USB to SMT machines, local file system)
- Can work offline using a local SQLite embedded in the MAUI app
- Relevant for factory-floor terminals that are not browser-based

**Cons:**
- MAUI toolchain is complex; macOS/iOS builds require a Mac or remote Mac agent
- No browser access — operators need an installed app
- Cross-platform testing matrix is much larger
- Heavy build infrastructure; not ideal for a coding-challenge deliverable

---

### Option D — Vertical Slice Architecture + Blazor WASM

Instead of horizontal layers, the API is organised around features — vertical slices. Each feature folder (`CreateOrder/`, `GetBoards/`, etc.) is self-contained: command/query, handler, validator, and API endpoint registration. MediatR dispatches requests. EF Core is used directly inside handlers.

**Projects:** `Api` (with feature folders inside) + `Blazor`

See [diagrams/option-d-vertical-slice.md](diagrams/option-d-vertical-slice.md)

**Pros:**
- Adding a feature = adding a folder; no need to touch five horizontal layers
- No generic repository boilerplate — EF Core `DbContext` is already a good abstraction for CRUD
- Handler unit tests are trivial (inject `DbContext` with in-memory provider)
- Strong interview talking point: demonstrates CQRS awareness and feature cohesion

**Cons:**
- Requires strong team conventions to stay consistent across slices
- MediatR is an additional dependency and concept to onboard
- Does not demonstrate the classic Repository pattern the challenge rubric likely expects
- Cross-cutting concerns (auth, caching) require MediatR pipeline behaviors — additional complexity

---

### Option E — Clean Architecture + Blazor Server

Robert C. Martin's Clean Architecture: concentric rings where inner rings have no dependency on outer rings. `Domain` (innermost) → `Application` → `Infrastructure` → `Web` (outermost). Blazor Server is used as the outer ring — single deployable, no REST API.

**Projects:** `Domain / Application / Infrastructure / Web`

See [diagrams/option-e-clean-arch.md](diagrams/option-e-clean-arch.md)

**Pros:**
- Strictest dependency inversion — `Domain` has zero external package dependencies
- Use-case classes are completely isolated from EF Core and ASP.NET
- Well-recognised Clean Architecture pattern — strong signal in senior developer interviews

**Cons:**
- Most ceremony for a 3-entity CRUD app — four projects before writing a line of business logic
- No REST API — JSON export requires a workaround (download action on a Blazor page)
- Same SignalR sticky-session concern as Option A
- Same microservices-roadmap dead end as Option A: requires full decomposition before Kubernetes

---

## Comparison

| Criterion | A — Blazor Server | B — API + Blazor WASM | C — API + MAUI | D — Vertical Slice | E — Clean Arch |
|---|---|---|---|---|---|
| Deploy complexity | Very low | Medium (2 processes) | High | Medium | Low |
| REST API included | No | **Yes** | **Yes** | **Yes** | No |
| JSON export story | Download action | Clean endpoint | Clean endpoint | Clean endpoint | Download action |
| Testability | Medium | **High** | High | High | High |
| Browser-accessible | Yes | Yes | No | Yes | Yes |
| Offline / native OS | No | No | Yes | No | No |
| Boilerplate | Low | Medium | Medium | Low–Medium | **High** |
| Pattern shown | Layered | **Hexagonal** | Hexagonal | CQRS / VSA | Clean Arch |
| Docker-composable | Partial | **Yes** | Partial | **Yes** | Partial |
| Microservices path | Full rewrite needed | **Extract services from API** | Extract services from API | Extract services from API | Full rewrite needed |
| Challenge fit | Good | **Excellent** | Fair | Very Good | Good |

---

## Recommendation

**Option B — Hexagonal REST API (core) + Optional Blazor WASM**

### Challenge requirements: mandatory vs optional

| Requirement | Status | Notes |
|---|---|---|
| CRUD for Orders, Boards, Components | **Mandatory** | Satisfied by REST API + Scalar UI |
| JSON export to production line | **Mandatory** | `POST /api/orders/{id}/export` via `IProductionLineExporter` |
| Persistence between restarts | **Mandatory** | PostgreSQL (see [ADR-002](adr/ADR-002-database.md)) |
| Version control (GitHub) | Optional | GitHub public/restricted repo |
| CI/CD pipeline | Optional | GitHub Actions — build + test on every commit |
| Web interface with authentication | Optional | Blazor WASM + auth layer; **login is required by the spec** |
| Dockerisation | Optional | `Dockerfile` + `docker-compose.yml` |
| Cloud deployment | Optional | Azure App Service / AKS |

### Rationale

The hexagonal internals ensure the domain is completely isolated from infrastructure — making it trivially testable today and straightforward to split into microservices later. The challenge spec calls for JSON export to the production line; modelling it as a named port (`IProductionLineExporter`) makes the simulation/real-adapter swap explicit and safe.

Scalar UI satisfies all mandatory CRUD interaction — `OrderManager.Blazor` is only added if time permits and only with an authentication layer, since the challenge spec explicitly requires login for any web interface.

The hexagonal structure maps directly to the project layout:

```
OrderManager.Core      →  Ports (domain models + interfaces)
OrderManager.Business  →  Application core (services — no infrastructure deps)
OrderManager.Data      →  Driven adapter (EF Core repositories)
OrderManager.Api       →  Driving adapter (HTTP / REST + Scalar UI)
OrderManager.Blazor    →  Optional HMI adapter (Blazor WASM) — requires auth
```

Dependency rule enforced at the MSBuild level via `.csproj` references:

```
Api       →  Business, Data, Core
Business  →  Core
Data      →  Core
Core      →  (no references)
Blazor (if added)  →  (HttpClient only — no server project references)
```

If a developer accidentally adds `OrderManager.Data` as a reference to `OrderManager.Business`, the build fails. Architecture violations become compile errors.

**12-factor config:** Connection strings, file export paths, and log levels are supplied via `appsettings.{Environment}.json` and overridable by environment variables — the standard `ASPNETCORE_ENVIRONMENT` / `ConnectionStrings__Default` pattern. No secrets are baked into the image; the same Docker image runs locally, in CI, and in Kubernetes without rebuilding.

### Alignment with the microservices roadmap

The API is already a clean stepping stone to microservices — splitting is additive, not a rewrite:

| Phase | State |
|---|---|
| **Now (challenge)** | Single `OrderManager.Api` Docker image + PostgreSQL container, orchestrated with Docker Compose |
| **Microservices** | Split `OrderManager.Api` along domain boundaries into `Order.Service`, `Board.Service`, `Component.Service`. Each service owns its database. API Gateway (YARP or Nginx) routes traffic. Deploy on Kubernetes. |

See [diagrams/future-microservices.md](diagrams/future-microservices.md) for the full Kubernetes diagram.

### Phased delivery

| Phase | Requirement | Scope |
|---|---|---|
| 1 | Mandatory | `Core + Business + Data + Api` — REST CRUD endpoints + Scalar UI |
| 2 | Mandatory | `Dockerfile` + `docker-compose.yml` wiring Api + PostgreSQL |
| 3 | Optional | GitHub Actions CI/CD — build + test on every commit |
| 4 (bonus) | Optional | `Blazor` WASM frontend + authentication (ASP.NET Core Identity or Azure AD B2C) |
| 5 (bonus) | Optional | Cloud deployment (Azure App Service / AKS) |

---

## Domain Events in Option B

### What would "event-driven" mean here?

Instead of a service directly calling `IProductionLineExporter.ExportAsync(order)`, the domain raises a named fact — `OrderReadyForProductionEvent` — and one or more handlers react to it independently. The service knows *what happened*; it does not know *who cares*.

At the in-process level this is still synchronous and in the same transaction. "Event-driven" here means the **design pattern**, not a message broker.

---

### Why it is a good idea

| Reason | Detail |
|---|---|
| **Open/Closed Principle** | Adding a new side-effect (e.g., send a Slack alert when an order is created) means adding a new handler — zero changes to `OrderService`. |
| **Explicit side-effects** | Every consequence of a domain action is a named class (`OrderCreatedEvent`, `ComponentStockDepletedEvent`). The list of handlers is the authoritative documentation of "what happens when X occurs." |
| **Natural fit for the production-line port** | The JSON export is already modelled as a port (`IProductionLineExporter`). Triggering it from an `OrderReadyForProduction` handler is more honest than calling it inside `CompleteOrder()` — the service no longer needs to know the export exists. |
| **Stepping stone to microservices** | In-process events can be promoted to a message broker (RabbitMQ, Azure Service Bus) with a single adapter swap. The handler code stays unchanged; only the transport changes. This makes the Option B → microservices migration additive rather than structural. |
| **Audit log for free** | A generic `DomainEventLogger` handler registered for all events gives a full timeline of domain facts without littering service code with log calls. |

---

### Why it is not a good idea

| Reason | Detail |
|---|---|
| **Ceremony for a 3-entity CRUD app** | Introducing an event dispatcher, event base classes, and handler registrations is meaningful boilerplate when `OrderService.Create()` has three lines of business logic. |
| **Debugging is indirect** | A call stack ending in `DispatchEvents()` then fanning out to N handlers is harder to follow than a direct method call. Exceptions inside a handler can be swallowed if the dispatcher catches them. |
| **Execution order is implicit** | If two handlers both react to `OrderCreated` and one depends on the side-effect of the other, the ordering must be encoded by convention (handler priority) — not by the type system. |
| **Transaction boundary confusion** | If the event is dispatched *before* `SaveChanges()` commits, a handler that reads from the database will not see the new state. If dispatched *after*, a handler failure cannot roll back the committed write. Both choices have failure modes. |
| **Test complexity** | A unit test for `OrderService.Create()` must now also assert that the correct event was raised — or, if the test mocks the dispatcher, it never verifies that the real handler wiring is correct. |

---

### How to implement it in Option B

**Step 1 — Domain event base and interface (in `Core`)**

```csharp
// Core/Events/IDomainEvent.cs
public interface IDomainEvent { }

// Core/Events/OrderCreatedEvent.cs
public record OrderCreatedEvent(Guid OrderId, string Name) : IDomainEvent;

// Core/Events/OrderReadyForProductionEvent.cs
public record OrderReadyForProductionEvent(Order Order) : IDomainEvent;
```

**Step 2 — Dispatcher port (in `Core`)**

```csharp
// Core/Ports/IDomainEventDispatcher.cs
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
```

**Step 3 — Entities collect events; services dispatch after SaveChanges**

```csharp
// Core/Models/Order.cs
public class Order
{
    private readonly List<IDomainEvent> _events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _events;

    public void Complete()
    {
        Status = OrderStatus.ReadyForProduction;
        _events.Add(new OrderReadyForProductionEvent(this));
    }

    public void ClearEvents() => _events.Clear();
}

// Business/Services/OrderService.cs
public async Task CompleteOrderAsync(Guid orderId, CancellationToken ct)
{
    var order = await _repo.GetByIdAsync(orderId, ct);
    order.Complete();                       // raises domain event internally
    await _repo.SaveChangesAsync(ct);       // commit first
    foreach (var e in order.DomainEvents)
        await _dispatcher.DispatchAsync(e, ct);  // then dispatch
    order.ClearEvents();
}
```

**Step 4 — In-process adapter (in `Business` or a thin `Infrastructure` project)**

Using MediatR (simplest):

```csharp
// Infrastructure/Events/MediatRDomainEventDispatcher.cs
public class MediatRDomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
{
    public Task DispatchAsync(IDomainEvent e, CancellationToken ct)
        => mediator.Publish(e, ct);  // MediatR INotification
}

// Business/EventHandlers/ExportOrderOnReadyHandler.cs
public class ExportOrderOnReadyHandler(IProductionLineExporter exporter)
    : INotificationHandler<OrderReadyForProductionEvent>
{
    public Task Handle(OrderReadyForProductionEvent e, CancellationToken ct)
        => exporter.ExportAsync(e.Order, ct);
}
```

**Step 5 — Promote to a message broker later (optional)**

Swap `MediatRDomainEventDispatcher` for a `RabbitMqDomainEventDispatcher` in DI. Handler code, service code, and domain entities are unchanged.

---

### Verdict for this project

> **Decision deferred.** Domain events are a candidate talking point for the coding challenge — the production-line export path (`OrderReadyForProductionEvent` → `ExportOrderOnReadyHandler`) is the natural showcase use case. Whether to include them, and to what extent, will be decided when the concrete architecture is designed. The transaction boundary trade-off (dispatch before vs. after `SaveChanges`) is the key design question to resolve at that point.
