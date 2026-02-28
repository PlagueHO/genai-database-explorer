# Data Model: Query Model with Agent Framework

**Date**: 2026-02-28
**Spec**: [spec.md](spec.md)
**Research**: [research.md](research.md)

## Entities

### QueryModelSettings

New settings section added to `ProjectSettings`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `AgentName` | `string` | `"genaidb-query-agent"` | Name for the Foundry agent version |
| `AgentInstructions` | `string?` | (see below) | System instructions for the agent. If null, uses default from prompt template |
| `MaxResponseRounds` | `int` | `10` | Maximum number of response rounds before forced termination |
| `MaxTokenBudget` | `long` | `100000` | Maximum cumulative token budget |
| `TimeoutSeconds` | `int` | `60` | Maximum wall-clock time in seconds |
| `DefaultTopK` | `int` | `5` | Default number of results per search tool call |

**Relationship**: Owned by `ProjectSettings` (1:1). Read by `SemanticModelQueryService` at initialization and per-query.

---

### SemanticModelQueryRequest

Represents a user's natural language query. Immutable record.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Question` | `string` | Yes | The natural language question |
| `TopK` | `int?` | No | Override for default top-K search results. Falls back to `QueryModelSettings.DefaultTopK` |

**Validation**: `Question` must not be null or whitespace.

---

### SemanticModelQueryResult

Structured outcome of processing a query. Immutable record.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Answer` | `string` | Yes | The agent's synthesized natural language answer |
| `ReferencedEntities` | `IReadOnlyList<SemanticModelSearchResult>` | Yes | Entities discovered during search (may be empty) |
| `ResponseRounds` | `int` | Yes | Number of response rounds executed |
| `InputTokens` | `long` | Yes | Total input tokens consumed |
| `OutputTokens` | `long` | Yes | Total output tokens consumed |
| `TotalTokens` | `long` | Yes | Total tokens consumed |
| `Duration` | `TimeSpan` | Yes | Wall-clock duration of the query |
| `TerminationReason` | `QueryTerminationReason` | Yes | Why the query loop ended |

---

### QueryTerminationReason

Enum describing why the agent loop terminated.

| Value | Description |
|-------|-------------|
| `Completed` | Agent produced a final answer naturally |
| `MaxRoundsReached` | Hit `MaxResponseRounds` limit |
| `TokenBudgetExceeded` | Hit `MaxTokenBudget` limit |
| `TimeLimitExceeded` | Hit `TimeoutSeconds` limit |
| `Error` | An error occurred (partial results returned) |

---

### SemanticModelSearchResult

A single entity match from vector search. Immutable record.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `EntityType` | `string` | Yes | `"Table"`, `"View"`, or `"StoredProcedure"` |
| `SchemaName` | `string` | Yes | Database schema (e.g., `"SalesLT"`) |
| `EntityName` | `string` | Yes | Entity name (e.g., `"Customer"`) |
| `Content` | `string` | Yes | Content description that was indexed |
| `Score` | `double` | Yes | Cosine similarity score (0.0–1.0) |

**Source**: Mapped from `(EntityVectorRecord Record, double Score)` tuples returned by `IVectorSearchService`.

---

## Service Interfaces

### ISemanticModelQueryService

Core service orchestrating agent-powered queries. Registered as singleton in DI. Implements `IAsyncDisposable` for agent cleanup.

| Method | Signature | Description |
|--------|-----------|-------------|
| `QueryAsync` | `Task<SemanticModelQueryResult> QueryAsync(SemanticModelQueryRequest request, CancellationToken ct)` | Execute a query and return the full result |
| `QueryStreamingAsync` | `Task<SemanticModelStreamingQueryResult> QueryStreamingAsync(SemanticModelQueryRequest request, CancellationToken ct)` | Start streaming answer tokens; returns a wrapper providing both the token stream and post-stream metadata |

**Dependencies**: `ISemanticModelSearchService`, `IProject`, `IChatClientFactory`, `IPromptTemplateParser`, `ILiquidTemplateRenderer`, `ILogger`

---

### SemanticModelStreamingQueryResult

Wrapper returned by `QueryStreamingAsync`. Provides access to the token stream and, once the stream ends, the full query metadata. Implements `IAsyncDisposable`.

| Member | Type | Description |
|--------|------|-------------|
| `Tokens` | `IAsyncEnumerable<string>` | Stream of answer text tokens for real-time display |
| `GetMetadataAsync()` | `Task<SemanticModelQueryResult>` | Completes after `Tokens` is fully enumerated. Returns the same structured result as `QueryAsync` (entities, rounds, token usage, termination reason) |

**Implementation note**: Internally backed by a `TaskCompletionSource<SemanticModelQueryResult>` that completes when the agent loop finishes.

---

### ISemanticModelSearchService

Wraps vector search with entity-type filtering. Used internally by the function tools. Registered as singleton.

| Method | Signature | Description |
|--------|-----------|-------------|
| `SearchTablesAsync` | `Task<IReadOnlyList<SemanticModelSearchResult>> SearchTablesAsync(string query, int topK, CancellationToken ct)` | Search for tables |
| `SearchViewsAsync` | `Task<IReadOnlyList<SemanticModelSearchResult>> SearchViewsAsync(string query, int topK, CancellationToken ct)` | Search for views |
| `SearchStoredProceduresAsync` | `Task<IReadOnlyList<SemanticModelSearchResult>> SearchStoredProceduresAsync(string query, int topK, CancellationToken ct)` | Search for stored procedures |

**Dependencies**: `IEmbeddingGenerator`, `IVectorSearchService`, `IVectorInfrastructureFactory`, `IProject`, `ILogger`

---

## State Transitions

### Query Lifecycle

```
[Idle] --QueryAsync()--> [Initializing]
  |                          |
  |                    (validate embeddings exist)
  |                          |
  |                   [Creating Session]
  |                          |
  |                  [Running Agent] <---(response round)---+
  |                    |       |                              |
  |              (text token) (function_call)                 |
  |                    |       |                              |
  |              [Streaming]  [Executing Tool]                |
  |                    |       |                              |
  |                    |  [Returning Result] -----------------+
  |                    |
  |             (guardrail check)
  |                    |
  |            [Completed / Terminated]
  |                    |
  +<----(return result)
```

### Agent Lifecycle (per service instance)

```
[Not Initialized] --InitializeAsync()--> [Creating Agent Version]
                                              |
                                        [Agent Ready] <--(reuse for queries)
                                              |
                                   --DisposeAsync()--> [Deleting Agent Version]
                                                            |
                                                       [Disposed]
```

## File Locations

| Artifact | Path |
|----------|------|
| `ISemanticModelQueryService` | `Core/SemanticModelQuery/ISemanticModelQueryService.cs` |
| `SemanticModelQueryService` | `Core/SemanticModelQuery/SemanticModelQueryService.cs` |
| `ISemanticModelSearchService` | `Core/SemanticModelQuery/ISemanticModelSearchService.cs` |
| `SemanticModelSearchService` | `Core/SemanticModelQuery/SemanticModelSearchService.cs` |
| `SemanticModelQueryRequest` | `Core/SemanticModelQuery/SemanticModelQueryRequest.cs` |
| `SemanticModelQueryResult` | `Core/SemanticModelQuery/SemanticModelQueryResult.cs` |
| `SemanticModelSearchResult` | `Core/SemanticModelQuery/SemanticModelSearchResult.cs` |
| `QueryTerminationReason` | `Core/SemanticModelQuery/QueryTerminationReason.cs` |
| `QueryModelSettings` | `Core/Models/Project/QueryModelSettings.cs` |
| `QueryModelCommandHandler` | `Console/CommandHandlers/QueryModelCommandHandler.cs` (update existing) |
| Agent instructions template | `Core/PromptTemplates/QueryModelAgent.prompt` |
