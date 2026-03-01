# Research: API Vector Search Endpoint

**Feature**: 007-api-vector-search
**Date**: 2026-03-01

## R1: How to wire vector search services into the API DI container

**Decision**: Register `ISemanticModelSearchService` and its dependencies in `Program.cs` by adding a dedicated registration block after `AddGenAIDBExplorerCoreServices()`.

**Rationale**: The API project already calls `builder.Services.AddGenAIDBExplorerCoreServices(builder.Configuration)` which registers `IProject`, `ISemanticModelRepository`, `IPerformanceMonitor`, and persistence strategies. The vector search services (`IVectorIndexPolicy`, `IVectorInfrastructureFactory`, `IEmbeddingGenerator`, `IVectorSearchService`, `ISemanticModelSearchService`) are currently only registered in the Console project's `HostBuilderExtensions.ConfigureServices()`. These must be added to the API's `Program.cs` or factored into a shared extension method in Core.

**Alternatives considered**:
- Move all vector service registrations into `ServiceRegistrationExtensions.AddGenAIDBExplorerCoreServices()` — rejected because generation services (`IVectorGenerationService`, `IVectorOrchestrator`, `IVectorIndexWriter`) are Console-only and would bloat API.
- Create a new `AddGenAIDBExplorerVectorSearchServices()` extension in Core — chosen, cleanly separates search-only services from generation services.

## R2: Minimum score threshold implementation

**Decision**: Apply a configurable minimum score threshold in the `ISemanticModelSearchService.SearchAsync()` unified method. Default threshold of 0.3 for cosine similarity (InMemory/CosmosDB). For Azure AI Search, the provider returns already-scored results where scores are normalized differently, so the threshold should be aligned per-provider.

**Rationale**: Cosine similarity scores in the -1 to 1 range (typically 0.0–1.0 for positive embeddings) mean a threshold of 0.3 filters out clearly irrelevant results while being permissive enough for broad queries. The existing `SemanticModelSearchService` already filters by entity type after search — adding a score threshold is a natural extension of the same post-filtering pattern.

**Alternatives considered**:
- Threshold at the `IVectorSearchService` level — rejected, would require changing a shared interface for an API-specific need.
- No threshold, just return everything — rejected per spec clarification session.

## R3: Cross-type search implementation in ISemanticModelSearchService

**Decision**: Add a `SearchAsync(string query, int topK, IReadOnlyList<string>? entityTypes, CancellationToken)` method to `ISemanticModelSearchService` that executes a single vector search against all entities and filters by type post-search.

**Rationale**: The existing per-type methods (`SearchTablesAsync`, `SearchViewsAsync`, `SearchStoredProceduresAsync`) each independently generate an embedding and search — calling all three would triple the embedding generation cost. A unified method generates the embedding once, performs one vector search with a larger topK, then filters and merges. The over-fetch multiplier pattern already handles this.

**Alternatives considered**:
- Call all three existing methods and merge — rejected due to 3x embedding cost and 3x search cost.
- Add three separate API endpoints (one per type) — rejected per spec clarification; single endpoint with optional filter is preferred.

## R4: API endpoint URL and method

**Decision**: `POST /api/search` with JSON body `{ "query": "...", "limit": 10, "entityTypes": ["table"] }`.

**Rationale**: POST is appropriate because: (a) the query could be arbitrarily long (up to embedding model limits), (b) POST body is the standard pattern for search APIs, (c) consistent with other search APIs (OpenSearch, Elasticsearch). The `/api/search` path is a top-level search resource, not nested under `/api/tables` since it spans all entity types.

**Alternatives considered**:
- `GET /api/search?query=...&limit=10` — rejected, long queries could hit URL length limits.
- `POST /api/entities/search` — viable but `/api/search` is simpler and the "entities" concept is implicit.

## R5: Test infrastructure for search endpoint

**Decision**: Extend `TestApiFactory` with a new `Mock<ISemanticModelSearchService>` property. The search endpoint injects `ISemanticModelSearchService` directly (not via `ISemanticModelCacheService`). Tests mock the search service to return controlled results.

**Rationale**: Follows the exact same pattern used for `MockCacheService`, `MockProject`, `MockRepository` in the existing `TestApiFactory`. The endpoint tests verify HTTP layer behavior (routing, validation, serialization) while the search service logic is tested separately in `SemanticModelSearchServiceTests`.

**Alternatives considered**:
- Test with real search service and mock lower-level services — rejected, endpoint tests should isolate the HTTP layer.

## R6: IChatClientFactory dependency for API

**Decision**: Register `IChatClientFactory` and `ChatClientEmbeddingGenerator` in the API's DI container, as required by `IEmbeddingGenerator` and `ISemanticModelSearchService`.

**Rationale**: The search endpoint needs to generate embeddings for query text, which requires `IEmbeddingGenerator` → `ChatClientEmbeddingGenerator` → `IChatClientFactory`. The API's `appsettings.json` will need OpenAI/Azure AI configuration just like the Console project (via project `settings.json`). However, the project settings already configures this via `IProject.Settings.MicrosoftFoundry` which is already loaded.

**Alternatives considered**:
- Pre-compute embeddings client-side — rejected, defeats the purpose of a search API.
