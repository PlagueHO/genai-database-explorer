# Implementation Plan: API Vector Search Endpoint

**Branch**: `007-api-vector-search` | **Date**: 2026-03-01 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/007-api-vector-search/spec.md`

## Summary

Expose a `POST /api/search` endpoint on the existing .NET 10 API that accepts a natural language query, generates an embedding, performs a vector similarity search against semantic model entities (tables, views, stored procedures), and returns a ranked list of up to 10 matching entities. The endpoint adds a unified `SearchAsync` method to the Core `ISemanticModelSearchService` for cross-type search with provider-aligned minimum score threshold filtering. The API layer remains thin — validating input, delegating to Core, and mapping results to response DTOs.

Key technical decisions (see [research.md](research.md)):
- **R1**: New `AddGenAIDBExplorerVectorSearchServices()` extension in Core for search-only DI (separates search from generation services)
- **R3**: Unified `SearchAsync` generates one embedding, performs one vector search, then filters by entity type and score post-search
- **R4**: `POST /api/search` with JSON body (supports long queries, standard search API pattern)
- **R5**: `TestApiFactory` extended with `Mock<ISemanticModelSearchService>` for endpoint test isolation

## Technical Context

**Language/Version**: .NET 10 / C# 14
**Primary Dependencies**: ASP.NET Core Minimal API, Microsoft.Extensions.AI (`IChatClientFactory`, `IEmbeddingGenerator`), Microsoft.SemanticKernel.Connectors.InMemory (vector store)
**Storage**: Vector indices (InMemory default, CosmosDB/Azure AI Search per settings); semantic model via `ISemanticModelRepository` (LocalDisk/AzureBlob/CosmosDB)
**Testing**: MSTest + FluentAssertions + Moq; `WebApplicationFactory<Program>` via `TestApiFactory`; `dotnet exec` (not `dotnet test` — .NET 10 SDK)
**Target Platform**: Linux/Windows server (ASP.NET Core Kestrel)
**Project Type**: Multi-project solution (API + Core library + Console CLI + Tests)
**Performance Goals**: Search response < 2s (dominated by embedding generation latency, provider-dependent)
**Constraints**: Max 10 results per request; single embedding call per search; minimum score threshold 0.3
**Scale/Scope**: Existing solution with ~50 source files; this feature adds ~6 new files and modifies ~4 existing files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Semantic Model Integrity | PASS | Search is read-only; no model mutations |
| II. AI Integration via Microsoft.Extensions.AI | PASS | Uses `IChatClientFactory` → `IEmbeddingGenerator` for query embedding; no new AI patterns introduced |
| III. Repository Pattern for Persistence | PASS | No new persistence; reads existing vector store via `IVectorSearchService` |
| IV. Project-Based Workflow | PASS | API loads project settings for vector infrastructure configuration |
| V. Test-First Development | PASS | Tests written before implementation per quickstart order |
| VI. CLI-First Interface | N/A | This is an API endpoint, not a CLI command |
| VII. Dependency Injection & Configuration | PASS | New `AddGenAIDBExplorerVectorSearchServices()` extension follows `ServiceRegistrationExtensions` pattern; called from `Program.cs` |
| Code Style | PASS | PascalCase types, camelCase JSON, `dotnet format` post-edit |
| Security | PASS | Input validation at endpoint boundary; no SQL; no secrets in responses |
| Naming Conventions | PASS | Test files `*Tests.cs`, records for DTOs, CosmosDB not abbreviated |

**Post-Phase 1 re-check**: All gates still PASS. Design adds a shared DI extension in Core (not a new project), stays within existing patterns.

## Project Structure

### Documentation (this feature)

```text
specs/007-api-vector-search/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions (R1–R6)
├── data-model.md        # Phase 1 data model definitions
├── quickstart.md        # Phase 1 implementation quickstart guide
├── contracts/
│   └── search-api.md    # Phase 1 API contract (POST /api/search)
├── checklists/
│   └── requirements.md  # Quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (changes to existing repository)

```text
genai-database-explorer-service/
├── src/
│   ├── GenAIDBExplorer.Api/
│   │   ├── Program.cs                              # MODIFY: add search DI + map search endpoints
│   │   ├── Endpoints/
│   │   │   └── SearchEndpoints.cs                   # NEW: POST /api/search endpoint
│   │   └── Models/
│   │       ├── SearchRequest.cs                     # NEW: request DTO record
│   │       ├── SearchResultResponse.cs              # NEW: single result DTO record
│   │       └── SearchResponse.cs                    # NEW: response wrapper DTO record
│   └── GenAIDBExplorer.Core/
│       ├── Extensions/
│       │   └── ServiceRegistrationExtensions.cs     # MODIFY: add AddGenAIDBExplorerVectorSearchServices()
│       └── SemanticModelQuery/
│           ├── ISemanticModelSearchService.cs        # MODIFY: add SearchAsync() method signature
│           └── SemanticModelSearchService.cs         # MODIFY: implement SearchAsync() with score threshold
└── tests/
    └── unit/
        ├── GenAIDBExplorer.Api.Test/
        │   ├── Infrastructure/
        │   │   └── TestApiFactory.cs                # MODIFY: add Mock<ISemanticModelSearchService>
        │   └── Endpoints/
        │       └── SearchEndpointsTests.cs          # NEW: endpoint HTTP tests
        └── GenAIDBExplorer.Core.Test/
            └── SemanticModelQuery/
                └── SemanticModelSearchServiceTests.cs  # MODIFY: add SearchAsync() tests
```

**Structure Decision**: Follows the existing multi-project layout. All new API files go in the `GenAIDBExplorer.Api` project under `Endpoints/` and `Models/` directories (consistent with `TableEndpoints.cs`, `EntitySummaryResponse.cs`, etc.). Core changes extend existing files in `SemanticModelQuery/` and `Extensions/`. No new projects are created.

## Complexity Tracking

No constitution violations. The design stays within existing patterns:
- No new projects added
- No new abstractions beyond `SearchAsync()` on an existing interface
- No new DI lifetimes (all singletons, same as Console registrations)
- No new dependencies (all NuGet packages already referenced)
