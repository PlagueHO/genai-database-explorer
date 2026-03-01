# Tasks: API Vector Search Endpoint

**Input**: Design documents from `specs/007-api-vector-search/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/search-api.md, quickstart.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in descriptions

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure shared across all user stories. Adds the unified search method to Core and registers search services in DI. No user story endpoint work can begin until this phase is complete.

### Tests for Foundation

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T001 Write unit tests for `SearchAsync()` unified method in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelSearchServiceTests.cs` — test cases: valid query returns ranked results, empty vector store returns empty list, score threshold filters low-relevance results, topK capping at 10, single embedding generation call per search. Follow existing test patterns (`SearchTablesAsync_WithResults_ReturnsExpectedResults` etc.) using AAA, Moq, FluentAssertions.

### Implementation for Foundation

- [X] T002 Add `SearchAsync(string query, int topK, IReadOnlyList<string>? entityTypes, CancellationToken)` method signature to `ISemanticModelSearchService` interface in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/ISemanticModelSearchService.cs` — returns `Task<IReadOnlyList<SemanticModelSearchResult>>`.

- [X] T003 Implement `SearchAsync()` in `SemanticModelSearchService` in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelSearchService.cs` — flow: validate inputs → create `VectorInfrastructure` from project settings → generate query embedding (single call) → vector search with `topK × OverFetchMultiplier` → filter by `entityTypes` (if provided) → apply minimum score threshold (0.3) → take topK → return ranked by score descending. Track token usage from the embedding generation call via structured logging (constitution II mandate). Reference existing `SearchByEntityTypeAsync` pattern.

- [X] T004 Add `AddGenAIDBExplorerVectorSearchServices()` extension method to `ServiceRegistrationExtensions` in `genai-database-explorer-service/src/GenAIDBExplorer.Core/Extensions/ServiceRegistrationExtensions.cs` — registers search-only services: `IChatClientFactory` → `ChatClientFactory`, `IVectorIndexPolicy` → `VectorIndexPolicy`, `IVectorInfrastructureFactory` → `VectorInfrastructureFactory`, `IEmbeddingGenerator` → `ChatClientEmbeddingGenerator`, `IVectorSearchService` → `SkInMemoryVectorSearchService`, `ISemanticModelSearchService` → `SemanticModelSearchService`, `InMemoryVectorStore`. All singletons. `IChatClientFactory` is required by `ChatClientEmbeddingGenerator` for query embedding generation (see research R6). Does NOT register generation services (`IVectorGenerationService`, `IVectorOrchestrator`, `IVectorIndexWriter`).

- [X] T005 Verify T001 tests pass. Build solution with `dotnet build genai-database-explorer-service/GenAIDBExplorer.slnx` and run Core tests with `dotnet exec genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/bin/Debug/net10.0/GenAIDBExplorer.Core.Test.dll`. Run `format-fix-whitespace-only` VS Code task.

**Checkpoint**: `ISemanticModelSearchService.SearchAsync()` works with unified cross-type search, score threshold filtering, and DI extension is ready. All existing tests still pass.

---

## Phase 2: User Story 1 — Search Entities by Natural Language (Priority: P1) 🎯 MVP

**Goal**: A consumer sends a natural language query to `POST /api/search` and receives a ranked list of matching semantic model entities (tables, views, stored procedures) with entity type, schema, name, description, and relevance score.

**Independent Test**: Send `POST /api/search` with `{"query": "customer orders"}` and verify a 200 response with ranked `SearchResultResponse` items in the response body.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T006 [P] [US1] Add `Mock<ISemanticModelSearchService>` property (named `MockSearchService`) to `TestApiFactory` in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Api.Test/Infrastructure/TestApiFactory.cs`. Follow existing pattern for `MockCacheService`, `MockProject`, `MockRepository`: add property, `RemoveAll<ISemanticModelSearchService>()` + `AddSingleton(MockSearchService.Object)` in `ConfigureWebHost`, reset mock in `ResetMocks()`.

- [X] T007 [P] [US1] Create `SearchEndpointsTests.cs` in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Api.Test/Endpoints/SearchEndpointsTests.cs` with test cases: `SearchEntities_ValidQuery_ReturnsOkWithResults` — mock `SearchAsync` to return 2 results, POST `{"query": "customer"}`, assert 200 + correct JSON structure; `SearchEntities_NoMatches_ReturnsEmptyResults` — mock returns empty, assert 200 + empty results array + `totalResults: 0`; `SearchEntities_ServiceThrowsException_Returns503` — mock throws, assert 503 with Problem Details. Use `PostAsJsonAsync`, `ReadFromJsonAsync`, `HttpStatusCode` assertions. Follow `TableEndpointsTests` pattern.

### Implementation for User Story 1

- [X] T008 [P] [US1] Create `SearchRequest` record in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Models/SearchRequest.cs` — fields: `string Query` (required), `int? Limit` (optional, default 10), `IReadOnlyList<string>? EntityTypes` (optional). Follow `UpdateEntityDescriptionRequest` pattern.

- [X] T009 [P] [US1] Create `SearchResultResponse` record in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Models/SearchResultResponse.cs` — fields: `string EntityType`, `string Schema`, `string Name`, `string Description`, `double Score`. Follow `EntitySummaryResponse` pattern.

- [X] T010 [P] [US1] Create `SearchResponse` record in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Models/SearchResponse.cs` — fields: `IReadOnlyList<SearchResultResponse> Results`, `int TotalResults`. Follow `PaginatedResponse<T>` pattern.

- [X] T011 [US1] Create `SearchEndpoints.cs` in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/SearchEndpoints.cs` — static class with `MapSearchEndpoints(this WebApplication app)` extension method. Maps `POST /api/search` via `MapGroup("/api/search").WithTags("Search")`. Handler `SearchEntities` injects `ISemanticModelSearchService` + `ILoggerFactory`, reads `SearchRequest` body, delegates to `SearchAsync()`, maps `SemanticModelSearchResult` to `SearchResultResponse`, returns `SearchResponse`. Produces metadata: `.Produces<SearchResponse>()`, `.ProducesProblem(400)`, `.ProducesProblem(503)`. Error handling: catch exceptions → 503 Problem Details (same pattern as `TableEndpoints`).

- [X] T012 [US1] Wire up search endpoint in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Program.cs` — add `builder.Services.AddGenAIDBExplorerVectorSearchServices()` after existing `AddGenAIDBExplorerCoreServices()` call, and add `app.MapSearchEndpoints()` alongside existing `MapTableEndpoints()`, `MapViewEndpoints()`, etc.

- [X] T013 [US1] Verify T006/T007 tests pass. Build solution and run API tests with `dotnet exec genai-database-explorer-service/tests/unit/GenAIDBExplorer.Api.Test/bin/Debug/net10.0/GenAIDBExplorer.Api.Test.dll`. Run `format-fix-whitespace-only` VS Code task.

**Checkpoint**: `POST /api/search` endpoint is live. A valid query returns ranked results. No matches returns empty array. Infrastructure errors return 503. All existing API tests still pass.

---

## Phase 3: User Story 2 — Limit Search Results (Priority: P1)

**Goal**: API consumer specifies max results (1–10, default 10, hard cap 10). System never returns more than 10 results.

**Independent Test**: Send `POST /api/search` with `{"query": "products", "limit": 5}` and verify ≤5 results. Send `{"query": "products", "limit": 15}` and verify ≤10 results.

### Tests for User Story 2

- [X] T014 [US2] Add test cases to `SearchEndpointsTests.cs` in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Api.Test/Endpoints/SearchEndpointsTests.cs`: `SearchEntities_LimitOf5_ReturnsAtMost5` — mock returns 10 results, POST with `limit: 5`, verify `SearchAsync` called with `topK: 5`; `SearchEntities_LimitAbove10_ClampedTo10` — POST with `limit: 15`, verify `SearchAsync` called with `topK: 10`; `SearchEntities_NoLimit_DefaultsTo10` — POST without limit, verify `SearchAsync` called with `topK: 10`; `SearchEntities_LimitZero_Returns400` — POST with `limit: 0`, assert 400 Problem Details; `SearchEntities_NegativeLimit_Returns400` — POST with `limit: -1`, assert 400 Problem Details.

### Implementation for User Story 2

- [X] T015 [US2] Add limit validation and clamping to `SearchEndpoints.SearchEntities` handler in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/SearchEndpoints.cs` — validate `limit < 1` → return 400 Problem Details; clamp `limit > 10` to 10; use `request.Limit ?? 10` for default. This builds on the endpoint created in T011.

- [X] T016 [US2] Verify T014 tests pass. Run API tests. Run `format-fix-whitespace-only`.

**Checkpoint**: Limit parameter works correctly: default 10, capped at 10, validation error for ≤0.

---

## Phase 4: User Story 3 — Filter Search by Entity Type (Priority: P2)

**Goal**: API consumer optionally filters by entity types (`"table"`, `"view"`, `"storedProcedure"`). When omitted, all types are searched.

**Independent Test**: Send `POST /api/search` with `{"query": "customers", "entityTypes": ["table"]}` and verify only Table results. Omit `entityTypes` and verify mixed types appear.

### Tests for User Story 3

- [X] T017 [US3] Add test cases to `SearchEndpointsTests.cs`: `SearchEntities_FilterByTable_PassesFilterToService` — POST with `entityTypes: ["table"]`, verify `SearchAsync` receives entity type filter; `SearchEntities_NoFilter_PassesNullToService` — POST without `entityTypes`, verify `SearchAsync` receives null; `SearchEntities_InvalidEntityType_Returns400` — POST with `entityTypes: ["invalid"]`, assert 400 Problem Details with message listing valid types.

- [X] T018 [US3] Add test case to `SemanticModelSearchServiceTests.cs` in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelSearchServiceTests.cs`: `SearchAsync_WithEntityTypeFilter_ReturnsOnlyMatchingTypes` — set up mixed entity type records, call `SearchAsync` with `entityTypes: ["Table"]`, assert only Table results returned.

### Implementation for User Story 3

- [X] T019 [US3] Add entity type validation to `SearchEndpoints.SearchEntities` handler in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/SearchEndpoints.cs` — validate each element of `entityTypes` is one of `"table"`, `"view"`, `"storedProcedure"` (case-insensitive). Return 400 Problem Details listing valid types if invalid value detected.

- [X] T020 [US3] Verify T017/T018 tests pass. Run both Core and API tests. Run `format-fix-whitespace-only`.

**Checkpoint**: Entity type filtering works: filter narrows results, no filter returns all types, invalid types rejected with 400.

---

## Phase 5: User Story 4 — Graceful Handling of Unavailable Search (Priority: P2)

**Goal**: When no embeddings exist, return empty results (not error). When infrastructure fails, return clear 503 error.

**Independent Test**: Mock search service to return empty list → verify 200 with empty results. Mock search service to throw → verify 503 with Problem Details.

### Tests for User Story 4

- [X] T021 [US4] Add test cases to `SearchEndpointsTests.cs`: `SearchEntities_EmptyVectorStore_ReturnsEmptyResults` — mock `SearchAsync` returns empty list, assert 200 + empty results; `SearchEntities_InfrastructureFailure_Returns503` — mock `SearchAsync` throws `InvalidOperationException`, assert 503 with Problem Details title "Service Unavailable" and descriptive detail.

- [X] T022 [US4] Add test case to `SemanticModelSearchServiceTests.cs`: `SearchAsync_EmptyVectorStore_ReturnsEmptyList` — set up mock vector search returning empty results, call `SearchAsync`, assert empty list returned (no exception thrown).

### Implementation for User Story 4

- [X] T023 [US4] Verify error handling in `SearchEndpoints.SearchEntities` covers infrastructure failures in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/SearchEndpoints.cs` — catch block returns 503 Problem Details with `title: "Service Unavailable"`, `detail: "The semantic model search service is not currently available."`, `type: "https://tools.ietf.org/html/rfc9110#section-15.6.4"` (same pattern as `TableEndpoints`). Log the exception with `ILoggerFactory`.

- [X] T024 [US4] Verify T021/T022 tests pass. Run both Core and API tests. Run `format-fix-whitespace-only`.

**Checkpoint**: Empty store returns zero results gracefully. Infrastructure errors produce clear, consistent 503 errors.

---

## Phase 6: Edge Cases & Input Validation

**Purpose**: Cover edge cases from spec: empty/whitespace queries, excessively long queries, and ensure all validation returns consistent 400 Problem Details.

### Tests

- [X] T025 [P] Add test cases to `SearchEndpointsTests.cs`: `SearchEntities_EmptyQuery_Returns400` — POST with `query: ""`, assert 400; `SearchEntities_WhitespaceQuery_Returns400` — POST with `query: "   "`, assert 400; `SearchEntities_NullQuery_Returns400` — POST with missing query field, assert 400; `SearchEntities_ExcessivelyLongQuery_Returns400` — POST with query > 2000 chars, assert 400.

### Implementation

- [X] T026 Add query validation to `SearchEndpoints.SearchEntities` in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/SearchEndpoints.cs` — check `string.IsNullOrWhiteSpace(request.Query)` → 400 "Query must not be empty."; check `request.Query.Length > 2000` → 400 "Query must not exceed 2000 characters." Return RFC 9457 Problem Details consistent with existing API.

- [X] T027 Verify T025 tests pass. Run API tests. Run `format-fix-whitespace-only`.

**Checkpoint**: All edge case validation is in place with clear Problem Details errors.

---

## Phase 7: Polish & Cross-Cutting

**Purpose**: Documentation, formatting, final validation.

- [X] T028 [P] Run `dotnet format genai-database-explorer-service/GenAIDBExplorer.slnx` and verify no formatting changes needed.
- [X] T029 [P] Ensure all new public types have XML doc comments: `SearchRequest`, `SearchResultResponse`, `SearchResponse`, `SearchEndpoints`, `AddGenAIDBExplorerVectorSearchServices()`.
- [X] T030 Run full solution build and all unit tests (both Core and API test projects) to confirm no regressions.
- [X] T031 Validate against quickstart.md — execute verification steps from `specs/007-api-vector-search/quickstart.md` to confirm end-to-end flow.

**Checkpoint**: All code formatted, documented, builds clean, all tests pass.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Foundation)**: No dependencies — start immediately. BLOCKS all subsequent phases.
- **Phase 2 (US1 — Search)**: Depends on Phase 1 completion. BLOCKS Phases 3–6 (endpoint must exist).
- **Phase 3 (US2 — Limit)**: Depends on Phase 2 (extends the endpoint handler).
- **Phase 4 (US3 — Filter)**: Depends on Phase 2. Can run in parallel with Phase 3.
- **Phase 5 (US4 — Errors)**: Depends on Phase 2. Can run in parallel with Phases 3–4.
- **Phase 6 (Edge Cases)**: Depends on Phase 2. Can run in parallel with Phases 3–5.
- **Phase 7 (Polish)**: Depends on all previous phases.

### Within Each Phase

- Tests MUST be written first and FAIL before implementation (constitution V)
- Models before services before endpoints
- Core before API
- Verify step confirms tests pass after implementation

### Parallel Opportunities

```
Phase 1 (Foundation) ──────────────────────────►
                                                 Phase 2 (US1: Search) ──────────►
                                                                                   ┌─ Phase 3 (US2: Limit) ─────►
                                                                                   ├─ Phase 4 (US3: Filter) ────►
                                                                                   ├─ Phase 5 (US4: Errors) ────►
                                                                                   └─ Phase 6 (Edge Cases) ─────►
                                                                                                                  Phase 7 (Polish) ──►
```

Within Phase 2: T006, T007, T008, T009, T010 can all run in parallel (different files).
Within Phase 6: T025 test writing is independent of implementation phases 3–5.
