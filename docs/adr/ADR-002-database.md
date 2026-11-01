# ADR-002 — Database: PostgreSQL

**Status:** Accepted  
**Date:** 2026-11-01

---

## Context

The domain requires relational storage for Orders, Boards, Components, and their junction tables (`OrderBoard`, `BoardComponent`). The application must be container-friendly and align with the company's Kubernetes roadmap.

SQLite was the initial candidate (mentioned in the challenge spec as a development option). PostgreSQL and SQL Server are the other candidates.

## Decision

**PostgreSQL** as the runtime database.

- Runs natively in a lightweight Linux container; integrates with Kubernetes Persistent Volume Claims.
- Handles concurrent multi-replica writes without file-level locking.
- Fully supported by EF Core via `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Available as a managed service on every major cloud (Azure Database for PostgreSQL, AWS RDS, GCP Cloud SQL).
- Swapping to SQL Server is a one-line connection string change — acceptable if a team already holds an MSSQL license.

### Per-environment provider

| Environment | Provider |
|---|---|
| `Development` (local) | SQLite (`appsettings.Development.json`) |
| `Production` / CI | PostgreSQL (connection string via env var `ConnectionStrings__Default`) |
| Unit tests | EF Core in-memory provider |

The hexagonal adapter pattern ensures zero business logic changes when switching providers — only the DI registration and connection string change.

## Consequences

- SQLite must **not** be used as the container runtime database: single-file locking causes concurrency failures when the API scales to multiple pods or is rescheduled to a different node.
- No secrets are baked into the Docker image; the same image runs locally, in CI, and in Kubernetes (12-factor config via env vars / `appsettings.{Environment}.json`).
- `dotnet ef migrations add` must be re-run whenever the schema changes.
