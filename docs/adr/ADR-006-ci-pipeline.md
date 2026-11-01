# ADR-006 — CI Pipeline: Single-Job GitHub Actions Workflow

**Status:** Accepted  
**Date:** 2026-11-01

---

## Context

The project needed a CI pipeline to verify that the solution builds and all tests pass on every push and pull request. Two structural questions arose: whether to split build and test into separate jobs, and which runner to use.

## Decision

Use a **single GitHub Actions job** with two sequential steps (build, then test) on a **GitHub-hosted `ubuntu-latest` runner**. Test results are published as a named GitHub Check using `dorny/test-reporter` so failures are visible without opening the console log.

## Rationale

### Single job vs. separate build and test jobs

Splitting into two jobs appears cleaner but has a hidden cost: `dotnet test` rebuilds the project by default. To avoid the double-build, the build job would need to upload the compiled `bin/` output as an artifact and the test job would download it — adding 30–60 seconds of artifact transfer overhead and additional workflow complexity with no real benefit at this project size.

The single-job approach avoids this by passing `--no-build` to `dotnet test`, which skips the rebuild because the previous step already compiled the solution. GitHub still shows which *step* failed (Build vs. Test), providing the same attribution clarity as separate jobs.

| Concern | Separate jobs | Single job (chosen) |
|---|---|---|
| Double-build | Requires artifact upload/download | Avoided with `--no-build` |
| Failure attribution | Per-job status badge | Per-step label in the run view |
| Workflow complexity | Higher (artifact actions) | Lower |
| Wall-clock time | Longer (transfer overhead) | Shorter |

### Runner: `ubuntu-latest`

.NET 10 is available on GitHub-hosted Ubuntu runners. Linux runners start faster and consume fewer free-tier minutes than Windows runners.

For projects that need persistent tooling — custom SDKs, hardware emulators, licensed compilers, or large cached dependencies — **self-hosted runners** are the appropriate solution. They avoid repeated installation time and allow artifact caching across jobs. That trade-off is out of scope for this challenge.

### Out of scope for this challenge

The following tools are standard in production pipelines but omitted here to keep the deliverable focused:

- **SonarQube / SonarCloud** — static analysis, technical debt tracking, and coverage gates integrated into the PR check
- **Microsoft SBOM Tool (`microsoft/sbom-tool`)** — generates a Software Bill of Materials (SBOM) enumerating all direct and transitive dependencies; a common requirement in enterprise and supply-chain security policies
- **Dependabot** — automated pull requests for dependency vulnerability patches

## Consequences

- Every push to `main` or any `feat/**` branch, and every pull request, triggers the pipeline.
- Test results appear as a dedicated **"MSTest Results"** check in the GitHub Checks tab, showing pass/fail counts and individual test names without requiring access to the raw log.
- The `TreatWarningsAsErrors` setting in `Directory.Build.props` means StyleCop violations and other code analysis warnings will fail the Build step, not just the Test step.
- If the pipeline needs to run migration commands or integration tests against a real database in the future, a PostgreSQL service container can be added to the same job without restructuring the workflow.
