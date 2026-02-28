# Implementation Plan: Query Model with Agent Framework

**Branch**: `005-query-model-agent` | **Date**: 2026-02-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-query-model-agent/spec.md`

## Summary

Add a `query-model` CLI command that accepts a natural language question and uses a Microsoft Agent Framework `AIAgent` backed by Foundry Agent Service to iteratively search the semantic model's vector index and synthesize an answer. The agent has three function tools (`searchTables`, `searchViews`, `searchStoredProcedures`) defined via `AIFunctionFactory.Create()` that wrap existing `IVectorSearchService` + `IEmbeddingGenerator` infrastructure. The core query service is centralized in `GenAIDBExplorer.Core` for reuse by the future API and web app.

**Key technical decisions** (see [research.md](research.md)):
- Use Microsoft Agent Framework (`AIAgent`, `RunStreamingAsync`, `AIFunctionFactory`) instead of manually implementing the Responses API loop
- Use Azure AI Project provider (`AIProjectClient.AsAIAgent()`) for Foundry Agent Service backend
- Agent version lifecycle: created once per service initialization, deleted on dispose
- Streaming from the start via `RunStreamingAsync()`
- Guardrails (time, tokens, rounds) enforced externally via `CancellationToken` + loop monitoring

## Technical Context

**Language/Version**: .NET 10 / C# 14
**Primary Dependencies**:
- `Microsoft.Agents.AI.OpenAI` (prerelease) — `AIAgent`, `AIFunctionFactory`, `AgentSession`, `RunStreamingAsync`
- `Azure.AI.Projects` 1.0.0-beta.5 — `AIProjectClient`, `AgentVersionCreationOptions`, `PromptAgentDefinition`
- `Azure.AI.Projects.OpenAI` 1.0.0-beta.5 — `AsAIAgent()` extension
- `Microsoft.Extensions.AI.OpenAI` 10.3.0 — `IChatClient`, `IEmbeddingGenerator`
- `Microsoft.SemanticKernel.Connectors.InMemory` 1.68.0-preview — in-memory vector store
- `System.CommandLine` 2.0.2 — CLI framework

**Storage**: Existing vector index (InMemory, CosmosDB, or Azure AI Search via `IVectorSearchService`)
**Testing**: MSTest + FluentAssertions + Moq (unit), Pester (integration)
**Target Platform**: Windows/Linux CLI, .NET 10
**Project Type**: Extends existing multi-project solution (Core library + Console app)
**Performance Goals**: Simple questions < 30s, complex multi-entity questions < 60s (SC-001)
**Constraints**: Configurable token budget (default 100K), timeout (default 60s), max rounds (default 10)
**Scale/Scope**: Operates on semantic models with hundreds to low-thousands of entities

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Semantic Model Integrity | PASS | Read-only access to semantic model; vector index queried, never modified |
| II. AI Integration via Semantic Kernel | **EVOLVING** | Constitution says "use ISemanticKernelFactory" but project has already migrated to `IChatClientFactory` (spec-001, spec-002). This feature adds Agent Framework as a new AI integration pattern alongside the existing `IChatClientFactory`. Both coexist — Agent Framework for agent orchestration, `IChatClientFactory` for embeddings. Constitution should be updated post-feature. |
| III. Repository Pattern for Persistence | PASS | No new persistence; reads existing vector index via `IVectorSearchService` |
| IV. Project-Based Workflow | PASS | `query-model` accepts `--project` parameter; `QueryModelSettings` from `settings.json` |
| V. Test-First Development | PASS | Unit tests for `SemanticModelSearchService`, `SemanticModelQueryService`, `QueryModelCommandHandler`; integration tests for CLI |
| VI. CLI-First Interface | PASS | New `query-model` command with `--question` and `--project` options (kebab-case) |
| VII. Dependency Injection & Configuration | PASS | Services registered in `HostBuilderExtensions`; settings via `IOptions<T>` pattern |

**Post-Phase 1 re-check**: Principle II note — the Agent Framework introduces a new AI integration pattern that supersedes the constitution's Semantic Kernel reference. The constitution was partially updated for spec-001/002 migration but should be fully updated to reference both `IChatClientFactory` (for direct AI operations) and `AIAgent` (for agent orchestration). This is **not a violation** — it's a natural evolution. No gate failure.

## Project Structure

### Documentation (this feature)

```text
specs/005-query-model-agent/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 research output
├── data-model.md        # Phase 1 data model
├── quickstart.md        # Phase 1 quickstart guide
├── contracts/           # Phase 1 API contracts
│   └── api-contracts.md
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
genai-database-explorer-service/
├── src/
│   ├── GenAIDBExplorer.Core/
│   │   ├── GenAIDBExplorer.Core.csproj          # Add Microsoft.Agents.AI.OpenAI package
│   │   ├── Models/Project/
│   │   │   ├── QueryModelSettings.cs            # NEW: Query agent configuration
│   │   │   └── ProjectSettings.cs               # MODIFY: Add QueryModel property
│   │   ├── SemanticModelQuery/                   # NEW: Entire directory
│   │   │   ├── ISemanticModelQueryService.cs     # NEW: Core query service interface
│   │   │   ├── SemanticModelQueryService.cs      # NEW: Agent orchestration implementation
│   │   │   ├── ISemanticModelSearchService.cs    # NEW: Search service interface
│   │   │   ├── SemanticModelSearchService.cs     # NEW: Vector search wrapper
│   │   │   ├── SemanticModelQueryRequest.cs      # NEW: Request record
│   │   │   ├── SemanticModelQueryResult.cs       # NEW: Result record
│   │   │   ├── SemanticModelSearchResult.cs      # NEW: Search result record
│   │   │   └── QueryTerminationReason.cs         # NEW: Termination enum
│   │   └── PromptTemplates/
│   │       └── QueryModelAgent.prompt            # NEW: Agent instructions template
│   └── GenAIDBExplorer.Console/
│       ├── CommandHandlers/
│       │   └── QueryModelCommandHandler.cs       # MODIFY: Implement query logic
│       ├── Extensions/
│       │   └── HostBuilderExtensions.cs          # MODIFY: Register query services
│       └── Resources/
│           └── LogMessages.resx                  # MODIFY: Add query-model log messages
└── tests/
    └── unit/
        └── GenAIDBExplorer.Core.Test/
            └── SemanticModelQuery/               # NEW: Test directory
                ├── SemanticModelQueryServiceTests.cs
                ├── SemanticModelSearchServiceTests.cs
                └── QueryModelSettingsTests.cs
    └── unit/
        └── GenAIDBExplorer.Console.Test/
            └── CommandHandlers/
                └── QueryModelCommandHandlerTests.cs  # NEW: Command handler tests
```

**Structure Decision**: Extends the existing Core + Console project structure. All query logic lives in a new `SemanticModelQuery/` namespace under Core. No new projects needed — this follows the existing pattern of feature directories within the Core library.

### Default Settings Template

Add to `DefaultProject/settings.json`:

```json
"QueryModel": {
    "AgentName": "genaidb-query-agent",
    "AgentInstructions": null,
    "MaxResponseRounds": 10,
    "MaxTokenBudget": 100000,
    "TimeoutSeconds": 60,
    "DefaultTopK": 5
}
```

## Implementation Approach

### Component Interaction

```
QueryModelCommandHandler
  │ (CLI: --project, --question)
  │
  ▼
ISemanticModelQueryService (SemanticModelQueryService)
  │
  ├─ On initialization:
  │   ├─ Creates AIProjectClient (from FoundryModels settings)
  │   ├─ Defines function tools via AIFunctionFactory.Create():
  │   │   ├─ searchTables → ISemanticModelSearchService.SearchTablesAsync
  │   │   ├─ searchViews → ISemanticModelSearchService.SearchViewsAsync
  │   │   └─ searchStoredProcedures → ISemanticModelSearchService.SearchStoredProceduresAsync
  │   └─ Creates AIAgent via aiProjectClient.CreateAIAgentAsync(name, model, instructions, tools)
  │
  ├─ On QueryStreamingAsync(request):
  │   ├─ Validates vector embeddings exist
  │   ├─ Creates AgentSession
  │   ├─ Starts CancellationTokenSource with timeout
  │   ├─ Calls agent.RunStreamingAsync(question, session)
  │   ├─ Yields text tokens from AgentResponseUpdate.Text
  │   ├─ Tracks rounds (FunctionCallContent items) and tokens
  │   └─ Breaks on guardrail: time, tokens, or rounds
  │
  └─ On DisposeAsync():
      └─ Calls aiProjectClient.Agents.DeleteAgent(agentName)

ISemanticModelSearchService (SemanticModelSearchService)
  │
  ├─ SearchTablesAsync(query, topK):
  │   ├─ IEmbeddingGenerator.GenerateAsync(query) → embedding vector
  │   ├─ IVectorSearchService.SearchAsync(vector, topK * 3) → results
  │   └─ Filter by EntityType == "Table", take topK → SemanticModelSearchResult[]
  │
  ├─ SearchViewsAsync(query, topK): [same pattern, EntityType == "View"]
  └─ SearchStoredProceduresAsync(query, topK): [same pattern, EntityType == "StoredProcedure"]
```

### Agent Instructions Template

The `QueryModelAgent.prompt` file provides the system instructions for the agent:

```yaml
---
name: query_model_agent
description: System instructions for the semantic model query agent
---
system:
You are a database schema expert assistant. You help users understand database schemas,
relationships, and business logic by searching a semantic model of the database.

You have access to three search tools:
- searchTables: Search for database tables
- searchViews: Search for database views
- searchStoredProcedures: Search for stored procedures

When answering questions:
1. Start by searching for the most relevant entity types
2. If initial results suggest related entities, search for those too
3. Synthesize your findings into a clear, structured answer
4. Reference specific entities (schema.name) in your answer
5. If you cannot find relevant information, explain what you searched for and suggest alternative terms

Database context: {{ database_name }} - {{ database_description }}
```

The `{{ database_name }}` and `{{ database_description }}` variables are sourced from the loaded `SemanticModel` metadata (populated during the `extract-model` phase).

### Key Design Decisions

1. **Agent Framework abstracts the ReAct loop**: We don't manually implement the response loop. `RunStreamingAsync()` internally handles function\_call → execute → function\_call\_output → repeat. The function tools are plain C# methods that the framework invokes automatically.

2. **Function tools capture service via closure**: The `AIFunctionFactory.Create()` methods need access to `ISemanticModelSearchService`. They are created as lambda closures over the injected service instance during `SemanticModelQueryService` initialization.

3. **Guardrails are external**: Since the Agent Framework manages the internal loop, we enforce limits by:
   - `CancellationTokenSource.CancelAfter(timeout)` for time limit
   - Counting `FunctionCallContent` items in the stream for round limit
   - Tracking cumulative tokens from update metadata for token budget
   - Breaking the `await foreach` loop when any limit is hit

4. **Entity type filtering via over-fetch**: The vector index is a single collection containing all entity types. Search tools over-fetch (topK × 3) and post-filter by entity type. This is simple and leverages the existing single-collection design.

5. **Lazy agent initialization**: The `AIAgent` and `AIProjectClient` are created on first use (first query), not during DI construction. This avoids blocking app startup with network calls.

## Complexity Tracking

No constitution violations requiring justification. The only note is that Principle II (AI Integration via Semantic Kernel) has evolved — the project already migrated to `IChatClientFactory` in spec-001/002, and this feature adds Agent Framework as an additional AI pattern. This is documented in the Constitution Check above.
