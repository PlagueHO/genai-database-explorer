# Implementation Plan: REST API for Semantic Model Repository

**Branch**: `003-api-semantic-model` | **Date**: 2026-02-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-api-semantic-model/spec.md`

## Summary

Add a new ASP.NET Core Minimal API project (`GenAIDBExplorer.Api`) that exposes the Semantic Model Repository as a REST API for front-end web applications. The API loads a pre-initialized project at startup, caches the semantic model in memory, and provides paginated CRUD endpoints for tables, views, and stored procedures. It integrates with the existing Aspire AppHost for health monitoring and uses the existing `GenAIDBExplorer.Core` project for all persistence operations.

## Technical Context

**Language/Version**: .NET 10 / C# 14
**Primary Dependencies**: ASP.NET Core Minimal APIs, `GenAIDBExplorer.Core`, `GenAIDBExplorer.ServiceDefaults`
**Storage**: Delegates to existing `ISemanticModelRepository` (LocalDisk/AzureBlob/CosmosDB)
**Testing**: MSTest + FluentAssertions + Moq (unit tests), `Microsoft.AspNetCore.Mvc.Testing` (integration tests)
**Target Platform**: Linux/Windows server, containerized via Aspire
**Project Type**: Web API (new project in existing solution)
**Performance Goals**: < 3s full model retrieval, 10 concurrent read requests without degradation
**Constraints**: Single project per API instance, no auth in initial version
**Scale/Scope**: ~15 endpoints, ~10 DTO types, 1 new service (cache), 1 health check

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Semantic Model Integrity | PASS | API reads from and writes to the model via `ISemanticModelRepository`. No direct file manipulation. Change tracking preserved. |
| II. AI Integration via Semantic Kernel | N/A | This feature does not invoke AI operations. AI-powered enrichment is deferred to a future async task API. |
| III. Repository Pattern for Persistence | PASS | All CRUD operations route through `ISemanticModelRepository` and its persistence strategies. |
| IV. Project-Based Workflow | PASS | API loads a project from configured path, using `IProject.LoadProjectConfiguration()`. |
| V. Test-First Development | PASS | Plan includes unit tests for all services, DTOs, and endpoints. Tests written before implementation per task ordering. |
| VI. CLI-First Interface | N/A | This feature adds a web API alongside the CLI. CLI remains the primary interface; API supplements it. |
| VII. Dependency Injection & Configuration | PASS | API uses shared service registrations extracted from `HostBuilderExtensions`. Configuration via `appsettings.json` and `IOptions<T>`. |
| Code Style | PASS | PascalCase types/methods, camelCase locals, `dotnet format` enforced. |
| Security | PASS | Input validation at API boundary. No SQL operations in the API layer. Auth deferred per spec. |
| Naming | PASS | `CosmosDb` naming convention followed. Project named `GenAIDBExplorer.Api`. |

**Post-Phase 1 Re-check**: All principles still pass. The addition of a new project (`GenAIDBExplorer.Api`) is justified in the Complexity Tracking table below.

## Project Structure

### Documentation (this feature)

```text
specs/003-api-semantic-model/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── api.md           # REST API contract
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
genai-database-explorer-service/
├── GenAIDBExplorer.slnx                         # Updated: add Api + Api.Test projects
├── src/
│   ├── GenAIDBExplorer.Api/                     # NEW: Web API project
│   │   ├── GenAIDBExplorer.Api.csproj
│   │   ├── Program.cs                           # App builder, middleware, endpoint mapping
│   │   ├── appsettings.json                     # Project path, CORS config
│   │   ├── appsettings.Development.json
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── Endpoints/                           # Minimal API endpoint definitions
│   │   │   ├── ModelEndpoints.cs                # GET /api/model, POST /api/model/reload
│   │   │   ├── TableEndpoints.cs                # GET/PATCH /api/tables
│   │   │   ├── ViewEndpoints.cs                 # GET/PATCH /api/views
│   │   │   ├── StoredProcedureEndpoints.cs      # GET/PATCH /api/stored-procedures
│   │   │   └── ProjectEndpoints.cs              # GET /api/project
│   │   ├── Models/                              # API-specific DTOs
│   │   │   ├── SemanticModelSummaryResponse.cs
│   │   │   ├── PaginatedResponse.cs
│   │   │   ├── EntitySummaryResponse.cs
│   │   │   ├── TableDetailResponse.cs
│   │   │   ├── ViewDetailResponse.cs
│   │   │   ├── StoredProcedureDetailResponse.cs
│   │   │   ├── ColumnResponse.cs
│   │   │   ├── IndexResponse.cs
│   │   │   ├── UpdateEntityDescriptionRequest.cs
│   │   │   └── ProjectInfoResponse.cs
│   │   ├── Services/                            # API-layer services
│   │   │   ├── ISemanticModelCacheService.cs    # Cache abstraction
│   │   │   └── SemanticModelCacheService.cs     # In-memory model cache with atomic swap
│   │   └── Health/                              # Custom health checks
│   │       └── SemanticModelHealthCheck.cs      # Reports model-loaded readiness
│   ├── GenAIDBExplorer.AppHost/                 # MODIFIED: register Api project
│   │   └── AppHost.cs
│   ├── GenAIDBExplorer.Console/                 # MODIFIED: extract shared DI registrations
│   │   └── Extensions/
│   │       └── HostBuilderExtensions.cs
│   ├── GenAIDBExplorer.Core/                    # MODIFIED: add shared DI registration extension
│   │   └── Extensions/
│   │       └── ServiceRegistrationExtensions.cs # Shared service registrations for Console + Api
│   └── GenAIDBExplorer.ServiceDefaults/         # UNCHANGED (consumed as-is)
└── tests/
    └── unit/
        └── GenAIDBExplorer.Api.Test/            # NEW: API unit tests
            ├── GenAIDBExplorer.Api.Test.csproj
            ├── Endpoints/                       # Endpoint tests using WebApplicationFactory
            │   ├── ModelEndpointsTests.cs
            │   ├── TableEndpointsTests.cs
            │   ├── ViewEndpointsTests.cs
            │   ├── StoredProcedureEndpointsTests.cs
            │   └── ProjectEndpointsTests.cs
            ├── Services/
            │   └── SemanticModelCacheServiceTests.cs
            └── Health/
                └── SemanticModelHealthCheckTests.cs
```

**Structure Decision**: New `GenAIDBExplorer.Api` project in the existing `src/` directory, following the same pattern as `GenAIDBExplorer.Console`. New test project `GenAIDBExplorer.Api.Test` in `tests/unit/`, following the existing `GenAIDBExplorer.Console.Test` and `GenAIDBExplorer.Core.Test` pattern. The API project references `GenAIDBExplorer.Core` (domain logic) and `GenAIDBExplorer.ServiceDefaults` (Aspire integration), same as the Console project.

## Implementation Phases

### Phase 1: Project Scaffolding & Core Infrastructure

Create the new API project, wire up DI, and establish the model caching service.

**Files created/modified**:

- `src/GenAIDBExplorer.Api/GenAIDBExplorer.Api.csproj` — New web API project (Microsoft.NET.Sdk.Web), referencing Core and ServiceDefaults
- `src/GenAIDBExplorer.Api/Program.cs` — App builder: AddServiceDefaults, configure CORS, configure JSON serialization, register SemanticModelCacheService, load project at startup, map health endpoints, map API endpoints
- `src/GenAIDBExplorer.Api/appsettings.json` — GenAIDBExplorer:ProjectPath, Cors:AllowedOrigins, Logging config
- `src/GenAIDBExplorer.Api/appsettings.Development.json` — Development overrides (permissive CORS)
- `src/GenAIDBExplorer.Api/Properties/launchSettings.json` — Local development URLs
- `src/GenAIDBExplorer.Api/Services/ISemanticModelCacheService.cs` — Interface: GetModelAsync(), ReloadModelAsync(), IsLoaded
- `src/GenAIDBExplorer.Api/Services/SemanticModelCacheService.cs` — Volatile reference + Interlocked.Exchange implementation
- `src/GenAIDBExplorer.Api/Health/SemanticModelHealthCheck.cs` — IHealthCheck: Healthy/Degraded/Unhealthy based on cache state
- `src/GenAIDBExplorer.Core/Extensions/ServiceRegistrationExtensions.cs` — NEW: Extract shared service registrations from Console's HostBuilderExtensions into Core
- `src/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs` — MODIFIED: call shared registration method from Core instead of inline registrations
- `src/GenAIDBExplorer.AppHost/AppHost.cs` — MODIFIED: add `builder.AddProject<Projects.GenAIDBExplorer_Api>("genaidbexplorer-api")`
- `GenAIDBExplorer.slnx` — MODIFIED: add Api and Api.Test projects

**Tests**:

- `tests/unit/GenAIDBExplorer.Api.Test/GenAIDBExplorer.Api.Test.csproj` — New test project
- `tests/unit/GenAIDBExplorer.Api.Test/Services/SemanticModelCacheServiceTests.cs` — Tests for cache load, reload, atomic swap, concurrent access
- `tests/unit/GenAIDBExplorer.Api.Test/Health/SemanticModelHealthCheckTests.cs` — Tests for health check states

**Validation**: `dotnet build` succeeds, `dotnet test` for Api.Test passes, health endpoint returns 200 when model loaded.

### Phase 2: DTO Models

Define all API response and request DTOs as C# records.

**Files created**:

- `src/GenAIDBExplorer.Api/Models/SemanticModelSummaryResponse.cs` — record
- `src/GenAIDBExplorer.Api/Models/PaginatedResponse.cs` — generic record
- `src/GenAIDBExplorer.Api/Models/EntitySummaryResponse.cs` — record
- `src/GenAIDBExplorer.Api/Models/TableDetailResponse.cs` — record
- `src/GenAIDBExplorer.Api/Models/ViewDetailResponse.cs` — record
- `src/GenAIDBExplorer.Api/Models/StoredProcedureDetailResponse.cs` — record
- `src/GenAIDBExplorer.Api/Models/ColumnResponse.cs` — record
- `src/GenAIDBExplorer.Api/Models/IndexResponse.cs` — record
- `src/GenAIDBExplorer.Api/Models/UpdateEntityDescriptionRequest.cs` — record with validation
- `src/GenAIDBExplorer.Api/Models/ProjectInfoResponse.cs` — record

**Validation**: Models compile, JSON serialization round-trip tests pass.

### Phase 3: Read-Only Endpoints (P1 + P2 stories)

Implement the GET endpoints for model, tables, views, stored procedures, and project.

**Files created**:

- `src/GenAIDBExplorer.Api/Endpoints/ModelEndpoints.cs` — `GET /api/model` (summary), `POST /api/model/reload`
- `src/GenAIDBExplorer.Api/Endpoints/TableEndpoints.cs` — `GET /api/tables` (paginated list), `GET /api/tables/{schema}/{name}` (detail)
- `src/GenAIDBExplorer.Api/Endpoints/ViewEndpoints.cs` — `GET /api/views` (paginated list), `GET /api/views/{schema}/{name}` (detail)
- `src/GenAIDBExplorer.Api/Endpoints/StoredProcedureEndpoints.cs` — `GET /api/stored-procedures` (paginated list), `GET /api/stored-procedures/{schema}/{name}` (detail)
- `src/GenAIDBExplorer.Api/Endpoints/ProjectEndpoints.cs` — `GET /api/project`

**Tests**:

- `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/ModelEndpointsTests.cs`
- `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/TableEndpointsTests.cs`
- `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/ViewEndpointsTests.cs`
- `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/StoredProcedureEndpointsTests.cs`
- `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/ProjectEndpointsTests.cs`

**Validation**: All GET endpoints return correct data. Pagination returns correct counts and offsets. 404 returned for non-existent entities. 503 returned when model not loaded.

### Phase 4: Write Endpoints (P3 story)

Implement the PATCH endpoints for updating entity descriptions.

**Files modified**:

- `src/GenAIDBExplorer.Api/Endpoints/TableEndpoints.cs` — Add `PATCH /api/tables/{schema}/{name}`
- `src/GenAIDBExplorer.Api/Endpoints/ViewEndpoints.cs` — Add `PATCH /api/views/{schema}/{name}`
- `src/GenAIDBExplorer.Api/Endpoints/StoredProcedureEndpoints.cs` — Add `PATCH /api/stored-procedures/{schema}/{name}`

**Tests**:

- Update existing endpoint test files with PATCH scenarios (update, not-found, validation errors)

**Validation**: PATCH endpoints update descriptions in the cached model and persist via repository. Changes survive API restart.

### Phase 5: CORS, OpenAPI & Polish

Configure CORS middleware, add OpenAPI documentation, finalize error handling.

**Files modified**:

- `src/GenAIDBExplorer.Api/Program.cs` — CORS policy from config, OpenAPI document generation, global exception handler producing Problem Details
- `src/GenAIDBExplorer.Api/appsettings.json` — CORS allowed origins config
- `src/GenAIDBExplorer.Api/appsettings.Development.json` — Permissive CORS for development

**Validation**: CORS preflight requests handled correctly. `/openapi/v1.json` returns valid OpenAPI document. All errors return RFC 9457 Problem Details format.

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| API framework | ASP.NET Core Minimal APIs | Small API surface, .NET 10 feature parity, less boilerplate than controllers |
| Model caching | Volatile reference + atomic swap | Simple single-writer/many-reader pattern, no lock contention for reads |
| Project loading | `appsettings.json` config | Mirrors CLI `--project` pattern, supports env var override |
| Pagination | Offset/limit | Simple, maps to Skip/Take on in-memory collections |
| Error format | RFC 9457 Problem Details | Industry standard, native .NET 10 support |
| Concurrency | Last-write-wins | Matches existing repository semaphore protection |
| DI registration | Shared extension method in Core | Avoids duplication between Console and Api projects; placed in Core to prevent circular project dependency |

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New project (GenAIDBExplorer.Api) | Web API requires ASP.NET Core Sdk.Web, different entry point than Console | Cannot add web API endpoints to an Exe console project without fundamentally changing its nature. Adding to Console would violate Single Responsibility. |
| New test project (GenAIDBExplorer.Api.Test) | API tests require `Microsoft.AspNetCore.Mvc.Testing` and WebApplicationFactory | Cannot test API endpoints without the ASP.NET test host infrastructure. |
