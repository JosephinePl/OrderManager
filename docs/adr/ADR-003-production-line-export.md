# ADR-003 — Production Line Export: Named Port (`IProductionLineExporter`)

**Status:** Accepted  
**Date:** 2026-11-01

---

## Context

The challenge requires exporting an order as JSON to the SMT production line. The real factory-floor transport (MQTT, IPC-CFX, file drop) is out of scope for the coding challenge deliverable but must not require business logic changes when it is introduced.

## Decision

Model the production line export as a named **port** in `Core`, with separate **adapters** registered per environment.

```csharp
// Core/Ports/IProductionLineExporter.cs
public interface IProductionLineExporter
{
    Task ExportAsync(Order order, CancellationToken ct = default);
}
```

| Environment | Adapter | Location |
|---|---|---|
| Development / challenge deliverable | `SimulatedFileExportAdapter` — writes JSON to a local file | `Data/Adapters/` |
| Production (future) | `MqttProductionLineAdapter` | `Infrastructure/Adapters/` |

The adapter is selected at startup via a single DI registration change — zero business logic changes.

JSON serialization uses `System.Text.Json` (no third-party dependency).

## Consequences

- Business logic in `OrderService` calls `IProductionLineExporter.ExportAsync()` and knows nothing about the transport.
- Swapping the simulation for a real factory-floor adapter is purely an infrastructure concern.
- The REST endpoint `POST /api/orders/{id}/export` is the natural trigger; the export logic is not embedded in a UI download action.
- Unit tests mock `IProductionLineExporter` — no file I/O or network required.
