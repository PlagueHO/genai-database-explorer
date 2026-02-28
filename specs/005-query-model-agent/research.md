# Research: Query Model with Agent Framework

**Date**: 2026-02-28
**Spec**: [spec.md](spec.md)

## R1: Microsoft Agent Framework vs. Raw Responses API

### Decision

Use **Microsoft Agent Framework** (`Microsoft.Agents.AI`) with the Azure AI Project provider instead of manually implementing the Responses API loop.

### Rationale

The spec originally described a manual response loop using `ProjectResponsesClient.CreateResponseStreaming()` with explicit `function_call` / `function_call_output` handling. The Agent Framework abstracts this entirely:

- `AIAgent` + `RunStreamingAsync()` handles the full ReAct loop internally (tool calls, local execution, result submission, repeat)
- `AIFunctionFactory.Create()` converts C# methods into agent function tools with zero boilerplate
- `AgentSession` provides built-in multi-turn conversation state for future use
- The framework is the official successor to Semantic Kernel and AutoGen, maintained by the same Microsoft teams

This eliminates FR-004 (manual response loop implementation) — the framework handles it.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Raw Responses API (`ProjectResponsesClient`) | Full control over loop, token tracking | Manual loop, error handling, streaming assembly |
| Agent Framework (`AIAgent`) | Abstracted loop, function tools via attributes, streaming built-in, sessions | Prerelease SDK, less granular control |
| Chat Completion API (`IChatClient`) | Already have factory, simpler | No agent management, manual tool loop |

**Rejected**: Raw Responses API because the Agent Framework provides the same capabilities with significantly less code. The framework internally uses the Responses API when backed by Azure AI Project.

### Impact on Spec

- **FR-002**: Still uses Foundry Agent Service, but via Agent Framework abstraction
- **FR-004**: Replaced — the framework manages the streaming response loop internally
- **FR-012**: Agent versions created via `AIProjectClient.Agents.CreateAgentVersion()` + `aiProjectClient.AsAIAgent()` (same SDK, framework wrapper)
- **FR-015**: Replaced — use `agent.RunStreamingAsync()` instead of `ProjectResponsesClient.CreateResponseStreaming()`
- **FR-003/FR-017**: Function tools defined via `AIFunctionFactory.Create()` with `[Description]` attributes instead of raw `FunctionTool` JSON schemas

---

## R2: Agent Creation Pattern with Azure AI Project Provider

### Decision

Use `AIProjectClient.CreateAIAgentAsync()` to create the agent with the Foundry backend, passing function tools at creation time via the Agent Framework's tool system.

### Rationale

The `Agent_With_AzureAIProject` sample demonstrates the canonical pattern:

```csharp
var aiProjectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());

// Option A: Create agent version manually, then wrap
var options = new AgentVersionCreationOptions(
    new PromptAgentDefinition(model: deploymentName) { Instructions = "..." });
var agentVersion = aiProjectClient.Agents.CreateAgentVersion(agentName: name, options: options);
AIAgent agent = aiProjectClient.AsAIAgent(agentVersion);

// Option B: Use convenience method
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    name: agentName,
    model: deploymentName,
    instructions: "...");
```

Function tools are provided either:
1. At agent creation via `tools` parameter
2. At run-time via `ChatClientAgentRunOptions` with `ChatOptions.Tools`

For our case, the search tools are static (same tools for every query), so providing them at agent creation is correct.

### Authentication

The existing `FoundryModelsDefaultSettings` provides the endpoint and authentication configuration. The `AIProjectClient` constructor accepts `TokenCredential` for Entra ID or can be configured with API key. We reuse the same authentication logic from `ChatClientFactory`.

---

## R3: Function Tool Definition Pattern

### Decision

Define three C# methods decorated with `[Description]` attributes and convert them to `AIFunction` instances via `AIFunctionFactory.Create()`.

### Rationale

The Agent Framework function tool pattern is:

```csharp
[Description("Search for tables in the semantic model matching the query.")]
static async Task<string> SearchTables(
    [Description("The search query text")] string query,
    [Description("Maximum number of results to return")] int topK = 5)
{
    // Execute vector search, return JSON results
}

var tools = new[]
{
    AIFunctionFactory.Create(searchTables),
    AIFunctionFactory.Create(searchViews),
    AIFunctionFactory.Create(searchStoredProcedures),
};
```

This is simpler than manually building `FunctionTool` JSON schemas (the original spec approach). The Agent Framework extracts the schema from the method signature and `[Description]` attributes automatically.

The three tools map directly to the spec's FR-003/FR-017:
- `searchTables` — searches `EntityType == "Table"` records
- `searchViews` — searches `EntityType == "View"` records
- `searchStoredProcedures` — searches `EntityType == "StoredProcedure"` records

Each tool internally uses `IEmbeddingGenerator` + `IVectorSearchService` but filters results by entity type.

---

## R4: Streaming Response Pattern

### Decision

Use `agent.RunStreamingAsync()` which returns `IAsyncEnumerable<AgentResponseUpdate>`. Each update's `.Text` property contains the incremental text tokens.

### Rationale

From the Agent Framework docs:

```csharp
await foreach (var update in agent.RunStreamingAsync("What is the weather?"))
{
    Console.Write(update.Text);
}
```

For our CLI, we write tokens as they arrive. For the Core service layer, we expose `IAsyncEnumerable<string>` to allow any consumer to process the stream.

The `AgentResponseUpdate.Contents` collection provides access to typed content items (`TextContent`, `FunctionCallContent`, `FunctionResultContent`) for detailed tracking.

### Token Tracking

Token usage in the Agent Framework is available via the response metadata. The `AgentResponse` (non-streaming) has `.Messages` which contain usage details. For streaming, we aggregate from the final update or from `Contents` metadata.

---

## R5: Agent Lifecycle Management

### Decision

Create the `AIAgent` (and its Foundry agent version) once during service initialization. Clean up on `IAsyncDisposable.DisposeAsync()`. Reuse across all queries.

### Rationale

Per spec clarification Q2: agent versions are per-startup, not per-query. The lifecycle is:

1. **Initialization**: `AIProjectClient` → `CreateAgentVersion()` → `AsAIAgent()` (or `CreateAIAgentAsync()`)
2. **Usage**: `agent.RunStreamingAsync(question, session)` for each query
3. **Cleanup**: `aiProjectClient.Agents.DeleteAgent(agentName)` on dispose

Sessions (`AgentSession`) are lightweight and created per-query for now (no multi-turn). The agent version persists across sessions.

---

## R6: NuGet Package Requirements

### Decision

Add `Microsoft.Agents.AI.OpenAI` (prerelease) to `GenAIDBExplorer.Core.csproj`. No changes to Console project needed beyond the existing project reference.

### Rationale

Current packages in Core that are relevant:
- `Azure.AI.Projects` 1.0.0-beta.5 — `AIProjectClient`, `AgentVersionCreationOptions`, `PromptAgentDefinition`
- `Azure.AI.Projects.OpenAI` 1.0.0-beta.5 — `AsAIAgent()` extension, `ProjectResponsesClient`
- `Microsoft.Extensions.AI.OpenAI` 10.3.0 — `IChatClient`, `IEmbeddingGenerator`

New package needed:
- `Microsoft.Agents.AI.OpenAI` (prerelease) — `AIAgent`, `AIFunctionFactory`, `AgentSession`, `RunStreamingAsync`, `AgentResponseUpdate`

This package provides the Agent Framework's `AsAIAgent()` extensions for OpenAI-based clients and the `AIFunctionFactory` for function tool creation.

---

## R7: Guardrail Implementation

### Decision

Implement guardrails as a wrapper around the Agent Framework's `RunStreamingAsync()` call using `CancellationTokenSource` with timeout and manual round/token counting.

### Rationale

The Agent Framework handles the internal tool loop, but doesn't expose configurable iteration limits. We implement guardrails externally:

1. **Time limit**: `CancellationTokenSource.CreateLinkedTokenSource()` with `CancelAfter(timeout)` — cancels the streaming enumeration
2. **Token budget**: Track cumulative tokens from `AgentResponseUpdate.Contents` metadata; if budget exceeded, break the streaming loop
3. **Round limit**: Track function call/result cycles from streamed content items; if max rounds exceeded, break the loop

When any guardrail fires, we capture the partial text gathered so far and return it with the termination reason.

---

## R8: Vector Search Integration

### Decision

Reuse existing `IVectorSearchService` and `IEmbeddingGenerator` infrastructure unchanged. The function tools are thin wrappers that call these services.

### Rationale

The existing infrastructure already provides:
- `IEmbeddingGenerator.GenerateAsync(text, infrastructure)` → embedding vector
- `IVectorSearchService.SearchAsync(vector, topK, infrastructure)` → ranked `(EntityVectorRecord, Score)` results
- `VectorInfrastructureFactory.Create(settings, strategy)` → infrastructure config
- Multiple backends: `InMemoryVectorSearchService`, `SkInMemoryVectorSearchService`

The function tools wrap these with entity type filtering:

```csharp
async Task<string> SearchTables(string query, int topK)
{
    var embedding = await _embeddingGenerator.GenerateAsync(query, _infrastructure);
    var results = await _vectorSearchService.SearchAsync(embedding, topK * 3, _infrastructure);
    var filtered = results.Where(r => r.Record.EntityType == "Table").Take(topK);
    return JsonSerializer.Serialize(filtered.Select(r => new { ... }));
}
```

The `topK * 3` over-fetch + filter approach ensures enough results of the specific entity type.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Separate vector collections per entity type | Exact results, no over-fetch | 3x index maintenance, 3x memory, complex infrastructure |
| Single collection with post-filter | Simple infrastructure, leverages existing | May need over-fetch to get enough results |
| Pre-filtered embedding search | Optimal | No metadata filtering in current search interface |

**Selected**: Single collection with post-filter — simplest to implement, adequate performance for expected data sizes.

---

## R9: Project Settings Extension for Query Configuration

### Decision

Add a `QueryModel` section to `ProjectSettings` for agent and guardrail configuration.

### Rationale

New settings needed:
- `QueryModel.AgentName` — name for the Foundry agent (default: `"genaidb-query-agent"`)
- `QueryModel.AgentInstructions` — system instructions for the agent (has default)
- `QueryModel.MaxResponseRounds` — max tool call rounds (default: 10)
- `QueryModel.MaxTokenBudget` — max total tokens (default: 100000)
- `QueryModel.TimeoutSeconds` — max wall-clock time (default: 60)
- `QueryModel.DefaultTopK` — default top-K for vector search (default: 5)

These fit the existing `settings.json` pattern alongside `FoundryModels`, `VectorIndex`, etc.
