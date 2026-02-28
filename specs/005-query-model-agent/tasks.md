# Tasks: Query Model with Agent Framework

**Input**: Design documents from `/specs/005-query-model-agent/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/api-contracts.md, quickstart.md

**Tests**: Included — constitution check V explicitly requires unit tests for `SemanticModelSearchService`, `SemanticModelQueryService`, and `QueryModelCommandHandler`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. User stories are ordered by dependency (US4 → US1 → US2) then priority.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Service source**: `genai-database-explorer-service/src/`
- **Core library**: `genai-database-explorer-service/src/GenAIDBExplorer.Core/`
- **Console app**: `genai-database-explorer-service/src/GenAIDBExplorer.Console/`
- **Unit tests**: `genai-database-explorer-service/tests/unit/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add NuGet package, create settings, DTOs, and enum types needed by all user stories

- [x] T001 Add `Microsoft.Agents.AI.OpenAI` NuGet package (prerelease) to `genai-database-explorer-service/src/GenAIDBExplorer.Core/GenAIDBExplorer.Core.csproj`
- [x] T002 [P] Create `QueryModelSettings` class with `AgentName`, `AgentInstructions`, `MaxResponseRounds`, `MaxTokenBudget`, `TimeoutSeconds`, `DefaultTopK` properties in `genai-database-explorer-service/src/GenAIDBExplorer.Core/Models/Project/QueryModelSettings.cs`
- [x] T003 [P] Write unit tests for `QueryModelSettings` defaults (`AgentName="genaidb-query-agent"`, `AgentInstructions=null`, `MaxResponseRounds=10`, `MaxTokenBudget=100000`, `TimeoutSeconds=60`, `DefaultTopK=5`) in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/QueryModelSettingsTests.cs`
- [x] T004 [P] Add `QueryModel` property of type `QueryModelSettings` to `ProjectSettings` in `genai-database-explorer-service/src/GenAIDBExplorer.Core/Models/Project/ProjectSettings.cs`
- [x] T005 [P] Add `QueryModel` section with defaults (including `AgentInstructions: null`) to `genai-database-explorer-service/src/GenAIDBExplorer.Core/DefaultProject/settings.json`
- [x] T006 [P] Create `QueryTerminationReason` enum (`Completed`, `MaxRoundsReached`, `TokenBudgetExceeded`, `TimeLimitExceeded`, `Error`) in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/QueryTerminationReason.cs`
- [x] T007 [P] Create `SemanticModelSearchResult` record (`EntityType`, `SchemaName`, `EntityName`, `Content`, `Score`) in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelSearchResult.cs`
- [x] T008 [P] Create `SemanticModelQueryRequest` record (`Question`, `TopK?`) in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryRequest.cs`
- [x] T009 [P] Create `SemanticModelQueryResult` record (`Answer`, `ReferencedEntities`, `ResponseRounds`, `InputTokens`, `OutputTokens`, `TotalTokens`, `Duration`, `TerminationReason`) in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryResult.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Prompt template and log messages that are needed before any service implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T010 Create `QueryModelAgent.prompt` template with system instructions for the database schema assistant agent (search tools usage, multi-step reasoning instructions, database context variables, `{{ database_name }}` and `{{ database_description }}` sourced from `SemanticModel` metadata) in `genai-database-explorer-service/src/GenAIDBExplorer.Core/PromptTemplates/QueryModelAgent.prompt`
- [x] T011 [P] Add query-model log messages (query started, query completed, agent created, agent disposed, guardrail triggered, search executed) to `genai-database-explorer-service/src/GenAIDBExplorer.Console/Resources/LogMessages.resx`

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 4 — Semantic Model Search via Vector Embeddings (Priority: P1) 🎯 MVP Foundation

**Goal**: Provide vector-based semantic model search with entity-type filtering, wrapping existing `IVectorSearchService` and `IEmbeddingGenerator` infrastructure

**Independent Test**: Call `SearchTablesAsync("customer data", 5)` directly and verify it returns ranked table entities from the vector index with metadata (type, schema, name, content, score)

### Tests for User Story 4

- [x] T012 [P] [US4] Write unit tests for `SemanticModelSearchService` covering: search returns filtered results by entity type, embedding generation is called, empty results handled, no-embeddings error case in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelSearchServiceTests.cs`

### Implementation for User Story 4

- [x] T013 [P] [US4] Create `ISemanticModelSearchService` interface with `SearchTablesAsync`, `SearchViewsAsync`, `SearchStoredProceduresAsync` methods in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/ISemanticModelSearchService.cs`
- [x] T014 [US4] Implement `SemanticModelSearchService` that generates embeddings via `IEmbeddingGenerator`, performs vector search via `IVectorSearchService` with over-fetch, filters by `EntityType`, maps `EntityVectorRecord` to `SemanticModelSearchResult`, returns top-K results in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelSearchService.cs`

**Checkpoint**: Search service can independently find and rank semantic model entities by type

---

## Phase 4: User Story 1 — Ask a Natural Language Question About the Database (Priority: P1) 🎯 MVP

**Goal**: Accept a natural language question and return an AI-synthesized answer using the Agent Framework with function tools backed by the search service

**Independent Test**: Call `QueryAsync(new SemanticModelQueryRequest("What tables store customer information?"))` and verify the result contains a relevant answer, referenced entities, round count, and token usage

### Tests for User Story 1

- [x] T015 [P] [US1] Write unit tests for `SemanticModelQueryService` covering: agent creation with function tools, basic query returns answer, streaming yields tokens, dispose cleans up agent, missing embeddings throws error, edge cases (empty question, unreachable endpoint, mid-loop error returns partial results) in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelQueryServiceTests.cs`

### Implementation for User Story 1

- [x] T016 [P] [US1] Create `ISemanticModelQueryService` interface with `QueryAsync` and `QueryStreamingAsync` methods, `SemanticModelStreamingQueryResult` wrapper class (exposes `Tokens` stream + `GetMetadataAsync()`), implementing `IAsyncDisposable` in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/ISemanticModelQueryService.cs`
- [x] T017 [US1] Implement `SemanticModelQueryService`: create `AIProjectClient` from `FoundryModelsSettings`, define three function tools via `AIFunctionFactory.Create()` capturing `ISemanticModelSearchService`, create agent via `CreateAIAgentAsync()`, implement `QueryStreamingAsync` returning `SemanticModelStreamingQueryResult` (backed by `TaskCompletionSource<SemanticModelQueryResult>`) using `agent.RunStreamingAsync()`, implement `QueryAsync` that collects streamed tokens into a complete result, implement `DisposeAsync` to delete agent version in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs`

**Checkpoint**: Core query service can accept a question, use the agent to search the semantic model, and return a structured answer

---

## Phase 5: User Story 2 — Agent Uses ReAct Loop to Iteratively Search (Priority: P1)

**Goal**: Verify and ensure the agent performs multi-round reasoning — issuing multiple function calls across response rounds to gather comprehensive information before answering

**Independent Test**: Ask "Explain the complete order management workflow" and verify the agent produces multiple response rounds before composing its answer

### Tests for User Story 2

- [x] T018 [P] [US2] Write multi-round reasoning test scenarios: verify `ResponseRounds > 1` for complex questions, verify follow-up searches when initial results suggest related entities, verify partial answer returned when no more rounds possible in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelQueryServiceTests.cs`

### Implementation for User Story 2

- [x] T019 [US2] Ensure `SemanticModelQueryService` correctly tracks `ResponseRounds` by counting `FunctionCallContent` items in the `AgentResponseUpdate` stream and populating `SemanticModelQueryResult.ResponseRounds` in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs`
- [x] T020 [US2] Ensure `SemanticModelQueryService` collects all `SemanticModelSearchResult` items from function tool executions into `SemanticModelQueryResult.ReferencedEntities` (deduplicated by schema + name) in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs`

**Checkpoint**: Agent performs multi-round reasoning and results include accurate round counts and referenced entities

---

## Phase 6: User Story 3 — Core Query Service is Reusable Across Interfaces (Priority: P2)

**Goal**: Wire the CLI command handler to delegate entirely to the Core query service, proving the architecture supports reuse across CLI, API, and web app

**Independent Test**: Run `query-model --project <path> --question "What tables store customer info?"` and verify the CLI streams the answer, displays referenced entities, and shows query statistics — with zero query logic in the Console project

### Tests for User Story 3

- [x] T021 [P] [US3] Write unit tests for `QueryModelCommandHandler` verifying it delegates to `ISemanticModelQueryService`, formats streaming output, displays metadata (entities, rounds, tokens), and handles edge cases (empty question, service unavailable) in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Console.Test/CommandHandlers/QueryModelCommandHandlerTests.cs`

### Implementation for User Story 3

- [x] T022 [US3] Register `ISemanticModelSearchService` as `SemanticModelSearchService` and `ISemanticModelQueryService` as `SemanticModelQueryService` (singletons) in DI container in `genai-database-explorer-service/src/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs`
- [x] T023 [US3] Update `QueryModelCommandHandler.HandleAsync` to: validate question input, call `ISemanticModelQueryService.QueryStreamingAsync`, iterate `result.Tokens` to stream answer to console, then `await result.GetMetadataAsync()` to display referenced entities and query statistics (rounds, tokens, duration, termination reason) in `genai-database-explorer-service/src/GenAIDBExplorer.Console/CommandHandlers/QueryModelCommandHandler.cs`

**Checkpoint**: CLI command works end-to-end, and Console project contains no query logic — only UI formatting and service delegation

---

## Phase 7: User Story 5 — Agent Termination and Safety Guardrails (Priority: P2)

**Goal**: Enforce configurable limits (time, tokens, rounds) so the agent cannot run indefinitely or consume excessive resources

**Independent Test**: Set `MaxResponseRounds=2` and ask a complex question; verify the agent terminates within 2 rounds and reports `TerminationReason.MaxRoundsReached`

### Tests for User Story 5

- [x] T024 [P] [US5] Write guardrail enforcement tests: timeout triggers `TimeLimitExceeded`, token budget triggers `TokenBudgetExceeded`, max rounds triggers `MaxRoundsReached`, partial results returned for all guardrail scenarios in `genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelQueryServiceTests.cs`

### Implementation for User Story 5

- [x] T025 [US5] Implement time limit via `CancellationTokenSource.CreateLinkedTokenSource()` with `CancelAfter(TimeoutSeconds)` in `SemanticModelQueryService` in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs`
- [x] T026 [US5] Implement token budget tracking by accumulating tokens from `AgentResponseUpdate` metadata and breaking the streaming loop when `MaxTokenBudget` exceeded in `SemanticModelQueryService` in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs`
- [x] T027 [US5] Implement max rounds enforcement by counting `FunctionCallContent` response round cycles and breaking when `MaxResponseRounds` exceeded in `SemanticModelQueryService` in `genai-database-explorer-service/src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs`

**Checkpoint**: All guardrails enforced; agent terminates gracefully within configured limits with correct termination reason

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Build verification, formatting, documentation, and final validation

- [x] T028 [P] Run `dotnet build` on solution and fix any compilation errors in `genai-database-explorer-service/GenAIDBExplorer.slnx`
- [x] T029 Run `dotnet format` (VS Code task `format-fix-whitespace-only`) across all changed `.cs` files
- [x] T030 Run `dotnet test` to verify all unit tests pass across solution in `genai-database-explorer-service/GenAIDBExplorer.slnx`
- [x] T031 [P] Create Pester integration tests for the `query-model` CLI command covering: basic question, missing project, missing embeddings error in `genai-database-explorer-service/tests/integration/`
- [x] T032 [P] Update CLI documentation with `query-model` command usage, options, and examples in `docs/cli/README.md`
- [x] T033 Validate quickstart.md scenarios: basic question, complex question, error scenarios (no embeddings, empty question) per `specs/005-query-model-agent/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US4 - Search (Phase 3)**: Depends on Phase 2 (DTOs, settings) — BLOCKS US1 and US2
- **US1 - Query (Phase 4)**: Depends on US4 (search service provides function tool implementations)
- **US2 - ReAct (Phase 5)**: Depends on US1 (extends query service with round tracking)
- **US3 - CLI (Phase 6)**: Depends on US1 (needs `ISemanticModelQueryService` to delegate to). Can run in parallel with US2 and US5.
- **US5 - Guardrails (Phase 7)**: Depends on US1 (extends query service with limit enforcement). Can run in parallel with US2 and US3.
- **Polish (Phase 8)**: Depends on all implementation phases being complete

### User Story Dependencies

```
Phase 1 (Setup) → Phase 2 (Foundational)
                       │
                       ▼
                  Phase 3 (US4: Search)
                       │
                       ▼
                  Phase 4 (US1: Query)
                  ╱    │    ╲
                 ╱     │     ╲
                ▼      ▼      ▼
    Phase 5   Phase 6   Phase 7
    (US2)     (US3)     (US5)
                ╲      │      ╱
                 ╲     │     ╱
                  ▼    ▼    ▼
                  Phase 8 (Polish)
```

### Within Each User Story

- Tests written FIRST (test-first per constitution principle V)
- Interfaces before implementations
- Models/DTOs before services
- Services before CLI integration
- Core implementation before integration verification

### Parallel Opportunities

**Phase 1**: T002–T009 can ALL run in parallel (independent files, no cross-dependencies)

**Phase 3**: T012 (tests) and T013 (interface) can run in parallel; T014 (implementation) depends on T013

**Phase 4**: T015 (tests) and T016 (interface) can run in parallel; T017 (implementation) depends on T016 and Phase 3

**Phases 5, 6, 7**: Can ALL start in parallel once Phase 4 is complete:
- US2 (response round tracking) modifies `SemanticModelQueryService`
- US3 (CLI) works on `QueryModelCommandHandler` and `HostBuilderExtensions`
- US5 (guardrails) modifies `SemanticModelQueryService`
- ⚠️ US2 and US5 both modify `SemanticModelQueryService` — if implemented in parallel, coordinate merges

**Phase 8**: T028, T031, and T032 can run in parallel

---

## Implementation Strategy

### MVP Scope

**Minimum Viable Product = Phase 1 + Phase 2 + Phase 3 (US4) + Phase 4 (US1) + Phase 6 (US3)**

This delivers:
- A working `query-model` CLI command
- Agent-powered question answering with search tools
- Streaming output to console
- Basic query results with metadata

MVP excludes multi-round tracking verification (US2) and configurable guardrails (US5), which can be added incrementally.

### Incremental Delivery

1. **Increment 1 (MVP)**: Phases 1–4 + Phase 6 → Working end-to-end query command
2. **Increment 2**: Phase 5 (US2) → Verified multi-round reasoning with round tracking
3. **Increment 3**: Phase 7 (US5) → Production-safe with configurable guardrails
4. **Increment 4**: Phase 8 → Polished, documented, validated

### New Files Created

| File | Phase | Description |
|------|-------|-------------|
| `Core/Models/Project/QueryModelSettings.cs` | 1 | Agent configuration settings |
| `Core/SemanticModelQuery/QueryTerminationReason.cs` | 1 | Termination reason enum |
| `Core/SemanticModelQuery/SemanticModelSearchResult.cs` | 1 | Search result record |
| `Core/SemanticModelQuery/SemanticModelQueryRequest.cs` | 1 | Query request record |
| `Core/SemanticModelQuery/SemanticModelQueryResult.cs` | 1 | Query result record |
| `Core/PromptTemplates/QueryModelAgent.prompt` | 2 | Agent system instructions |
| `Core/SemanticModelQuery/ISemanticModelSearchService.cs` | 3 | Search service interface |
| `Core/SemanticModelQuery/SemanticModelSearchService.cs` | 3 | Search service implementation |
| `Core/SemanticModelQuery/ISemanticModelQueryService.cs` | 4 | Query service interface + `SemanticModelStreamingQueryResult` |
| `Core/SemanticModelQuery/SemanticModelQueryService.cs` | 4 | Query service implementation |
| `tests/.../SemanticModelSearchServiceTests.cs` | 3 | Search service tests |
| `tests/.../SemanticModelQueryServiceTests.cs` | 4 | Query service tests |
| `tests/.../QueryModelSettingsTests.cs` | 1 | Settings tests |
| `tests/.../QueryModelCommandHandlerTests.cs` | 6 | Command handler tests |

### Modified Files

| File | Phase | Change |
|------|-------|--------|
| `Core/GenAIDBExplorer.Core.csproj` | 1 | Add `Microsoft.Agents.AI.OpenAI` package |
| `Core/Models/Project/ProjectSettings.cs` | 1 | Add `QueryModel` property |
| `Core/DefaultProject/settings.json` | 1 | Add `QueryModel` section |
| `Console/Extensions/HostBuilderExtensions.cs` | 6 | Register query services |
| `Console/CommandHandlers/QueryModelCommandHandler.cs` | 6 | Implement query delegation |
| `Console/Resources/LogMessages.resx` | 2 | Add log messages |
| `docs/cli/README.md` | 8 | Document `query-model` command |
