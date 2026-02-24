# Tasks: REST API for Semantic Model Repository

**Input**: Design documents from `/specs/003-api-semantic-model/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included — the constitution mandates test-first development (Principle V). Tests are written before implementation within each user story.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

All source paths are relative to `genai-database-explorer-service/`:

- **API source**: `src/GenAIDBExplorer.Api/`
- **API tests**: `tests/unit/GenAIDBExplorer.Api.Test/`
- **Console (modified)**: `src/GenAIDBExplorer.Console/`
- **Core (modified)**: `src/GenAIDBExplorer.Core/`
- **AppHost (modified)**: `src/GenAIDBExplorer.AppHost/`
- **Solution file**: `GenAIDBExplorer.slnx`

### Phase Mapping (tasks → plan)

| Tasks Phase | Plan Phase | Scope |
|-------------|------------|-------|
| Phase 1: Setup | Phase 1: Scaffolding | Project files, config |
| Phase 2: Foundational | Phase 1: Scaffolding + Phase 2: DTOs | Cache, health, DI, shared DTOs |
| Phases 3–6: US1–US4 | Phase 3: Read-Only Endpoints | GET endpoints per entity type |
| Phase 7: US5 | Phase 4: Write Endpoints | PATCH endpoints |
| Phase 8: US6 | Phase 3: Read-Only Endpoints | GET /api/project |
| Phase 9: Polish | Phase 5: CORS, OpenAPI & Polish | Cross-cutting concerns |

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the new API project, test project, and basic configuration files

- [X] T001 Create API project file `src/GenAIDBExplorer.Api/GenAIDBExplorer.Api.csproj` with Microsoft.NET.Sdk.Web targeting net10.0, referencing GenAIDBExplorer.Core and GenAIDBExplorer.ServiceDefaults
- [X] T002 [P] Create API test project file `tests/unit/GenAIDBExplorer.Api.Test/GenAIDBExplorer.Api.Test.csproj` with MSTest.Sdk targeting net10.0, referencing GenAIDBExplorer.Api, FluentAssertions, Moq, and Microsoft.AspNetCore.Mvc.Testing
- [X] T003 Add GenAIDBExplorer.Api and GenAIDBExplorer.Api.Test projects to `GenAIDBExplorer.slnx`
- [X] T004 [P] Create `src/GenAIDBExplorer.Api/appsettings.json` with GenAIDBExplorer:ProjectPath, Cors:AllowedOrigins, and Logging configuration
- [X] T005 [P] Create `src/GenAIDBExplorer.Api/appsettings.Development.json` with permissive CORS and debug logging overrides
- [X] T006 [P] Create `src/GenAIDBExplorer.Api/Properties/launchSettings.json` with HTTP/HTTPS local development URLs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented — cache service, health check, shared DI, Program.cs skeleton, and shared DTOs

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 Create `src/GenAIDBExplorer.Api/Services/ISemanticModelCacheService.cs` with GetModelAsync(), ReloadModelAsync(), and IsLoaded property
- [X] T008 Create `src/GenAIDBExplorer.Api/Services/SemanticModelCacheService.cs` implementing ISemanticModelCacheService with volatile reference and Interlocked.Exchange for atomic model swap
- [X] T009 [P] Create `src/GenAIDBExplorer.Api/Health/SemanticModelHealthCheck.cs` implementing IHealthCheck, returning Healthy/Degraded/Unhealthy based on cache state
- [X] T010 Create `src/GenAIDBExplorer.Core/Extensions/ServiceRegistrationExtensions.cs` extracting shared Core service registrations (repository, persistence strategies, project, caching, performance monitoring) from Console's HostBuilderExtensions
- [X] T011 Refactor `src/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs` to call shared registration method from `src/GenAIDBExplorer.Core/Extensions/ServiceRegistrationExtensions.cs` instead of inline registrations
- [X] T012 Create `src/GenAIDBExplorer.Api/Program.cs` skeleton with AddServiceDefaults, shared DI, SemanticModelCacheService registration, health check registration, JSON serialization config, structured request logging middleware (FR-013), and MapDefaultEndpoints
- [X] T013 Register Api project in Aspire AppHost by adding `builder.AddProject<Projects.GenAIDBExplorer_Api>("genaidbexplorer-api")` in `src/GenAIDBExplorer.AppHost/AppHost.cs`
- [X] T014 [P] Create shared DTO `src/GenAIDBExplorer.Api/Models/PaginatedResponse.cs` as generic record with Items, TotalCount, Offset, Limit properties
- [X] T015 [P] Create shared DTO `src/GenAIDBExplorer.Api/Models/EntitySummaryResponse.cs` as record with Schema, Name, Description, SemanticDescription, NotUsed properties
- [X] T016 [P] Create shared DTO `src/GenAIDBExplorer.Api/Models/ColumnResponse.cs` as record mapping from SemanticModelColumn properties
- [X] T017 [P] Create shared DTO `src/GenAIDBExplorer.Api/Models/IndexResponse.cs` as record mapping from SemanticModelIndex properties
- [X] T018 [P] Write tests for SemanticModelCacheService (load, reload, atomic swap, concurrent access, failure recovery) in `tests/unit/GenAIDBExplorer.Api.Test/Services/SemanticModelCacheServiceTests.cs`
- [X] T019 [P] Write tests for SemanticModelHealthCheck (healthy when loaded, unhealthy when not loaded, degraded during reload) in `tests/unit/GenAIDBExplorer.Api.Test/Health/SemanticModelHealthCheckTests.cs`

**Checkpoint**: Foundation ready — `dotnet build` succeeds, `dotnet test` passes for cache and health check tests, health endpoint returns 200 when model loaded. User story implementation can now begin.

---

## Phase 3: User Story 1 — Retrieve the Semantic Model (Priority: P1) 🎯 MVP

**Goal**: A front-end application can retrieve the full semantic model summary and trigger a model reload

**Independent Test**: Make a GET request to `/api/model` and verify the response contains model name, source, description, and entity counts. POST to `/api/model/reload` and verify the model is refreshed.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T020 [US1] Write ModelEndpointsTests (GET /api/model returns summary, POST /api/model/reload refreshes cache, 503 when model not loaded) in `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/ModelEndpointsTests.cs`

### Implementation for User Story 1

- [X] T021 [P] [US1] Create `src/GenAIDBExplorer.Api/Models/SemanticModelSummaryResponse.cs` as record with Name, Source, Description, TableCount, ViewCount, StoredProcedureCount
- [X] T022 [US1] Implement ModelEndpoints with MapModelEndpoints extension method (GET /api/model, POST /api/model/reload with Problem Details 503 response when model not loaded) in `src/GenAIDBExplorer.Api/Endpoints/ModelEndpoints.cs`
- [X] T023 [US1] Register model endpoints by calling MapModelEndpoints in `src/GenAIDBExplorer.Api/Program.cs`

**Checkpoint**: User Story 1 fully functional — GET /api/model returns model summary, POST /api/model/reload refreshes the cache, 503 returned when model not loaded.

---

## Phase 4: User Story 2 — Browse Individual Tables (Priority: P1)

**Goal**: A front-end application can list all tables with pagination and retrieve full details of a specific table including columns and indexes

**Independent Test**: GET `/api/tables?offset=0&limit=10` returns a paginated list. GET `/api/tables/SalesLT/Product` returns full table details with columns and indexes.

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T024 [US2] Write TableEndpointsTests (GET list with pagination, GET detail with columns/indexes, 404 for missing table, 503 when model not loaded, pagination edge cases) in `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/TableEndpointsTests.cs`

### Implementation for User Story 2

- [X] T025 [P] [US2] Create `src/GenAIDBExplorer.Api/Models/TableDetailResponse.cs` as record with Schema, Name, Description, SemanticDescription, SemanticDescriptionLastUpdate, Details, AdditionalInformation, NotUsed, NotUsedReason, Columns, Indexes
- [X] T026 [US2] Implement TableEndpoints with MapTableEndpoints extension method (GET /api/tables with offset/limit, GET /api/tables/{schema}/{name} with path parameter validation and Problem Details error responses) in `src/GenAIDBExplorer.Api/Endpoints/TableEndpoints.cs`
- [X] T027 [US2] Register table endpoints by calling MapTableEndpoints in `src/GenAIDBExplorer.Api/Program.cs`

**Checkpoint**: User Stories 1 AND 2 fully functional — tables can be listed and individually retrieved with all details.

---

## Phase 5: User Story 3 — Browse Individual Views (Priority: P2)

**Goal**: A front-end application can list all views with pagination and retrieve full details of a specific view including columns and SQL definition

**Independent Test**: GET `/api/views?offset=0&limit=10` returns a paginated list. GET `/api/views/SalesLT/vProductAndDescription` returns full view details with columns and definition.

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T028 [US3] Write ViewEndpointsTests (GET list with pagination, GET detail with columns/definition, 404 for missing view, 503 when model not loaded) in `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/ViewEndpointsTests.cs`

### Implementation for User Story 3

- [X] T029 [P] [US3] Create `src/GenAIDBExplorer.Api/Models/ViewDetailResponse.cs` as record with Schema, Name, Description, SemanticDescription, SemanticDescriptionLastUpdate, AdditionalInformation, Definition, NotUsed, NotUsedReason, Columns
- [X] T030 [US3] Implement ViewEndpoints with MapViewEndpoints extension method (GET /api/views with offset/limit, GET /api/views/{schema}/{name} with path parameter validation and Problem Details error responses) in `src/GenAIDBExplorer.Api/Endpoints/ViewEndpoints.cs`
- [X] T031 [US3] Register view endpoints by calling MapViewEndpoints in `src/GenAIDBExplorer.Api/Program.cs`

**Checkpoint**: User Stories 1, 2, AND 3 fully functional — views can be listed and individually retrieved.

---

## Phase 6: User Story 4 — Browse Individual Stored Procedures (Priority: P2)

**Goal**: A front-end application can list all stored procedures with pagination and retrieve full details of a specific stored procedure including parameters and SQL definition

**Independent Test**: GET `/api/stored-procedures?offset=0&limit=10` returns a paginated list. GET `/api/stored-procedures/dbo/uspGetCustomers` returns full stored procedure details with parameters and definition.

### Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T032 [US4] Write StoredProcedureEndpointsTests (GET list with pagination, GET detail with parameters/definition, 404 for missing procedure, 503 when model not loaded) in `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/StoredProcedureEndpointsTests.cs`

### Implementation for User Story 4

- [X] T033 [P] [US4] Create `src/GenAIDBExplorer.Api/Models/StoredProcedureDetailResponse.cs` as record with Schema, Name, Description, SemanticDescription, SemanticDescriptionLastUpdate, AdditionalInformation, Parameters, Definition, NotUsed, NotUsedReason
- [X] T034 [US4] Implement StoredProcedureEndpoints with MapStoredProcedureEndpoints extension method (GET /api/stored-procedures with offset/limit, GET /api/stored-procedures/{schema}/{name} with path parameter validation and Problem Details error responses) in `src/GenAIDBExplorer.Api/Endpoints/StoredProcedureEndpoints.cs`
- [X] T035 [US4] Register stored procedure endpoints by calling MapStoredProcedureEndpoints in `src/GenAIDBExplorer.Api/Program.cs`

**Checkpoint**: All read-only stories (US1–US4) fully functional — complete browsing experience for model, tables, views, and stored procedures.

---

## Phase 7: User Story 5 — Update Entity Descriptions (Priority: P3)

**Goal**: A front-end application can update the description and semantic description of tables, views, and stored procedures, with changes persisted through the repository

**Independent Test**: PATCH `/api/tables/SalesLT/Product` with a new description, then GET the same table and verify the description changed. Restart the API and verify the change persisted.

### Tests for User Story 5

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T036 [P] [US5] Create `src/GenAIDBExplorer.Api/Models/UpdateEntityDescriptionRequest.cs` as record with nullable Description and SemanticDescription properties, plus validation
- [X] T037 [US5] Add PATCH test scenarios (update description, update semantic description, 404 for missing entity, 400 for invalid input) to `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/TableEndpointsTests.cs`
- [X] T038 [P] [US5] Add PATCH test scenarios to `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/ViewEndpointsTests.cs`
- [X] T039 [P] [US5] Add PATCH test scenarios to `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/StoredProcedureEndpointsTests.cs`

### Implementation for User Story 5

- [X] T040 [US5] Implement PATCH /api/tables/{schema}/{name} in `src/GenAIDBExplorer.Api/Endpoints/TableEndpoints.cs` with input validation, model update, and repository persistence
- [X] T041 [P] [US5] Implement PATCH /api/views/{schema}/{name} in `src/GenAIDBExplorer.Api/Endpoints/ViewEndpoints.cs` with input validation, model update, and repository persistence
- [X] T042 [P] [US5] Implement PATCH /api/stored-procedures/{schema}/{name} in `src/GenAIDBExplorer.Api/Endpoints/StoredProcedureEndpoints.cs` with input validation, model update, and repository persistence

**Checkpoint**: Full CRUD for entity descriptions — updates are persisted and survive API restarts.

---

## Phase 8: User Story 6 — View Project Configuration (Priority: P3)

**Goal**: A front-end application can view the current project configuration to understand which database project and persistence strategy the API is serving

**Independent Test**: GET `/api/project` returns project path, model name, database source, and persistence strategy.

### Tests for User Story 6

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T043 [US6] Write ProjectEndpointsTests (GET /api/project returns config, 503 when model not loaded) in `tests/unit/GenAIDBExplorer.Api.Test/Endpoints/ProjectEndpointsTests.cs`

### Implementation for User Story 6

- [X] T044 [P] [US6] Create `src/GenAIDBExplorer.Api/Models/ProjectInfoResponse.cs` as record with ProjectPath, ModelName, ModelSource, PersistenceStrategy, ModelLoaded
- [X] T045 [US6] Implement ProjectEndpoints with MapProjectEndpoints extension method (GET /api/project) in `src/GenAIDBExplorer.Api/Endpoints/ProjectEndpoints.cs`
- [X] T046 [US6] Register project endpoints by calling MapProjectEndpoints in `src/GenAIDBExplorer.Api/Program.cs`

**Checkpoint**: All 6 user stories fully functional — complete API surface implemented.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Finalize middleware, documentation, error handling, and validation across all endpoints

- [X] T047 [P] Configure CORS middleware with config-driven allowed origins in `src/GenAIDBExplorer.Api/Program.cs`
- [X] T048 [P] Configure OpenAPI document generation at /openapi/v1.json in `src/GenAIDBExplorer.Api/Program.cs`
- [X] T049 Add global exception handler producing RFC 9457 Problem Details for unhandled exceptions in `src/GenAIDBExplorer.Api/Program.cs`
- [X] T050 Run quickstart.md validation scenarios (health check, model retrieval, table listing, table detail, description update, reload, project info) and verify SC-001 (< 3s response) and SC-005 (10 concurrent reads) meet success criteria
- [X] T051 Run full test suite with `dotnet test` and verify solution builds cleanly with `dotnet build`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phases 3–8)**: All depend on Foundational phase completion
  - US1 (Phase 3) and US2 (Phase 4) are both P1 priority — implement in order for MVP
  - US3 (Phase 5) and US4 (Phase 6) are both P2 — can proceed in parallel after Foundational
  - US5 (Phase 7) depends on US2/US3/US4 (needs existing endpoint files to add PATCH)
  - US6 (Phase 8) can start after Foundational — no dependency on other user stories
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational — no dependencies on other stories
- **US2 (P1)**: Can start after Foundational — no dependencies on other stories
- **US3 (P2)**: Can start after Foundational — no dependencies on other stories
- **US4 (P2)**: Can start after Foundational — no dependencies on other stories
- **US5 (P3)**: Depends on US2, US3, US4 (adds PATCH to existing endpoint files)
- **US6 (P3)**: Can start after Foundational — no dependencies on other stories

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- DTOs before endpoint implementation
- Endpoint implementation before Program.cs registration
- Story complete before moving to next priority

### Parallel Opportunities

- T001/T002 can run in parallel (API project + test project)
- T004/T005/T006 can run in parallel (config files, no dependencies)
- T014/T015/T016/T017 can run in parallel (shared DTOs, independent files)
- T018/T019 can run in parallel (test files, independent)
- US3 (Phase 5) and US4 (Phase 6) can run in parallel (independent endpoint groups)
- US5 PATCH tests (T037/T038/T039) can run in parallel after T036
- US5 PATCH implementations (T040/T041/T042) can run in parallel within the phase
- US6 can run in parallel with US3/US4/US5 (independent endpoints)

---

## Parallel Example: Setup Phase

```text
# Launch project creation in parallel:
T001: Create GenAIDBExplorer.Api.csproj
T002: Create GenAIDBExplorer.Api.Test.csproj

# Then update solution (depends on T001 + T002):
T003: Add projects to GenAIDBExplorer.slnx

# Launch config files in parallel:
T004: Create appsettings.json
T005: Create appsettings.Development.json
T006: Create launchSettings.json
```

## Parallel Example: Foundational Shared DTOs

```text
# Launch all shared DTOs in parallel:
T014: Create PaginatedResponse.cs
T015: Create EntitySummaryResponse.cs
T016: Create ColumnResponse.cs
T017: Create IndexResponse.cs

# Launch foundational tests in parallel:
T018: Write SemanticModelCacheServiceTests.cs
T019: Write SemanticModelHealthCheckTests.cs
```

## Parallel Example: User Stories 3 & 4

```text
# After Foundational phase, launch both P2 stories in parallel:
# Developer A: US3 (Views)
T028 → T029 → T030 → T031

# Developer B: US4 (Stored Procedures)
T032 → T033 → T034 → T035
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup
1. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
1. Complete Phase 3: US1 — Retrieve Semantic Model
1. Complete Phase 4: US2 — Browse Individual Tables
1. **STOP and VALIDATE**: Test model retrieval and table browsing independently
1. Deploy/demo if ready — front-end can browse the semantic model and tables

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
1. Add US1 → Test independently → Deploy/Demo (minimal MVP: model summary)
1. Add US2 → Test independently → Deploy/Demo (MVP: model + tables)
1. Add US3 + US4 → Test independently → Deploy/Demo (full browsing)
1. Add US5 → Test independently → Deploy/Demo (editing capability)
1. Add US6 → Test independently → Deploy/Demo (project info context)
1. Polish → Final deployment

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
1. Once Foundational is done:
   - Developer A: US1 → US2 → US5 (core + tables + write endpoints)
   - Developer B: US3 → US4 → US6 (views + stored procedures + project config)
1. Team Polish phase together

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- After modifying any `*.cs` file, run the `format-fix-whitespace-only` VS Code task
- All DTOs are C# records per data-model.md
- Endpoint files use static `Map*Endpoints(this WebApplication app)` extension methods per Minimal API conventions
