# Research: REST API for Semantic Model Repository

**Feature**: 003-api-semantic-model
**Date**: 2026-02-23

## Research Tasks

### 1. ASP.NET Core Minimal API vs Controllers for .NET 10

**Decision**: ASP.NET Core Minimal APIs
**Rationale**: The API surface is relatively small (< 20 endpoints) and CRUD-focused. Minimal APIs in .NET 10 have full feature parity with controllers for this scope (parameter binding, validation, OpenAPI, Problem Details, CORS). They produce less boilerplate, align with the project's preference for slim service projects, and are the recommended approach for new .NET APIs.
**Alternatives considered**:

- Controllers: More ceremony, separate controller classes. Beneficial for very large APIs with complex model binding or filters. Not needed here.
- Carter: Third-party minimal API library. Unnecessary overhead since native minimal APIs are sufficient.

### 2. Semantic Model Caching Strategy

**Decision**: Thread-safe singleton service (`SemanticModelCacheService`) holding a `volatile` reference to an immutable `SemanticModel` snapshot, swapped atomically via `Interlocked.Exchange` on reload.
**Rationale**: The `SemanticModel` class is loaded once at startup and served to all read requests. Using a volatile reference + atomic swap ensures in-flight reads complete against the old model while new reads pick up the refreshed model — matching the edge case requirement for atomic model switching.
**Alternatives considered**:

- `IMemoryCache`: Adds indirection and expiration semantics we don't need. The model has no TTL; it's refreshed explicitly.
- `ReaderWriterLockSlim`: Heavier locking. Atomic reference swap is simpler and sufficient for a single-writer (reload) / many-reader pattern.

### 3. Project Configuration Loading for the API

**Decision**: The API loads the project path from its own `appsettings.json` configuration (`GenAIDBExplorer:ProjectPath`). At startup, it calls `IProject.LoadProjectConfiguration()` with this path, then loads the semantic model from the configured persistence strategy.
**Rationale**: This mirrors the CLI pattern where each command receives a `--project` path. For the API, configuring it once at startup via `appsettings.json` (or environment variable override) is the natural equivalent. It reuses the existing `IProject` and `ISemanticModelRepository` infrastructure.
**Alternatives considered**:

- Command-line argument: Possible but less conventional for a web API. Can still be supported via `args` in `Program.cs`.
- Environment variable only: Too restrictive. `appsettings.json` gives full Configuration provider chain.

### 4. Health Check Integration with Aspire

**Decision**: Register a custom `IHealthCheck` (`SemanticModelHealthCheck`) that:

- Returns `Healthy` when the model is loaded and ready
- Returns `Degraded` when a reload is in progress
- Returns `Unhealthy` when the model failed to load

Register it with the existing `AddDefaultHealthChecks()` from `ServiceDefaults`. The `MapDefaultEndpoints()` already maps `/health` and `/alive`.
**Rationale**: The `ServiceDefaults` project already provides health check infrastructure with OpenTelemetry tracing exclusion for health endpoints. Adding a custom health check integrates naturally. The AppHost `AddProject<>()` call automatically wires up Aspire's health monitoring.
**Alternatives considered**:

- Separate health endpoint outside of Aspire's framework: Duplicates effort and loses Aspire dashboard integration.

### 5. DI Service Registration Reuse

**Decision**: Extract the core service registrations (repository, persistence strategies, caching, performance monitoring) into a shared extension method in `GenAIDBExplorer.Core` or a new shared registration class, so both the Console and API projects can register the same services without duplication.
**Rationale**: The HostBuilderExtensions in the Console project registers ~40 services. The API needs a subset of these (primarily repository/persistence/caching services, not command handlers or database providers). Extracting the shared registrations avoids copy-paste and ensures consistency.
**Alternatives considered**:

- Full copy of HostBuilderExtensions into API: High duplication, drift risk.
- API references Console project: Circular dependency risk and pulls in unnecessary CLI dependencies.

### 6. Pagination Implementation

**Decision**: Offset/limit query parameters with defaults (`offset=0`, `limit=50`). Response includes `totalCount`, `offset`, `limit`, and `items[]`. Maximum limit capped at 200.
**Rationale**: Simple, well-understood pagination pattern. Offset/limit maps directly to `.Skip()/.Take()` on in-memory collections. Cursor-based pagination would add complexity without benefit since the data is cached in memory and doesn't change between pages.
**Alternatives considered**:

- Cursor-based: More complex, designed for mutable datasets. Our cached model is immutable between reloads.
- No pagination: Clarification session chose pagination for future-proofing.

### 7. CORS Configuration

**Decision**: Configure CORS via `appsettings.json` with a named policy. Default to permissive in development (`*` origins), restricted in production (configured origins list).
**Rationale**: Standard ASP.NET Core CORS middleware. Configuration-driven allows deployment flexibility without code changes.
**Alternatives considered**:

- Hardcoded origins: Inflexible for deployment.
- No CORS (reverse proxy handles it): Valid for production, but API should still support direct development use.

### 8. OpenAPI / Swagger Documentation

**Decision**: Include built-in OpenAPI support via `Microsoft.AspNetCore.OpenApi` (included in .NET 10 framework). Generate OpenAPI document at `/openapi/v1.json`. Optionally include Scalar UI for interactive exploration.
**Rationale**: .NET 10 has built-in OpenAPI document generation for minimal APIs. This provides front-end developers with a machine-readable API contract and interactive documentation with minimal configuration.
**Alternatives considered**:

- Swashbuckle: Being deprecated in favor of the built-in OpenAPI support.
- No OpenAPI: Misses opportunity for front-end developer productivity.
