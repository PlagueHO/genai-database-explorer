# Quickstart: Query Model with Agent Framework

**Date**: 2026-02-28
**Spec**: [spec.md](spec.md)

## Prerequisites

1. .NET 10 SDK installed
2. Azure AI Foundry project with a deployed chat completion model (e.g., `gpt-4o-mini`)
3. Azure AI Foundry project with a deployed embedding model (e.g., `text-embedding-3-large`)
4. Project `settings.json` configured with `FoundryModels` endpoint and deployment names
5. Semantic model extracted and enriched (`extract-model` + `enrich-model`)
6. Vector embeddings generated (`generate-vectors`)

## Configuration

Add the `QueryModel` section to your project's `settings.json`:

```json
{
    "QueryModel": {
        "AgentName": "genaidb-query-agent",
        "MaxResponseRounds": 10,
        "MaxTokenBudget": 100000,
        "TimeoutSeconds": 60,
        "DefaultTopK": 5
    }
}
```

All settings have reasonable defaults. The section is optional — omitting it uses all defaults.

## Usage

### CLI

```bash
# Ask a simple question
dotnet run --project src/GenAIDBExplorer.Console/ -- query-model \
    --project D:/myproject \
    --question "What tables store customer information?"

# Ask a complex question
dotnet run --project src/GenAIDBExplorer.Console/ -- query-model \
    --project D:/myproject \
    --question "Explain the complete order management workflow including tables, views, and stored procedures"
```

### Output

The CLI progressively streams the answer as tokens arrive:

```
Querying semantic model...

The customer information is stored in several tables:

1. **SalesLT.Customer** - Contains core customer data including...
2. **SalesLT.CustomerAddress** - Links customers to their addresses...

Referenced Entities:
  - [Table] SalesLT.Customer (score: 0.92)
  - [Table] SalesLT.CustomerAddress (score: 0.87)
  - [Table] SalesLT.Address (score: 0.81)

Query Statistics:
  Response Rounds: 3
  Tokens: 1,245 input / 342 output / 1,587 total
  Duration: 4.2s
  Termination: Completed
```

### Programmatic (Core Library)

```csharp
// Inject ISemanticModelQueryService via DI
var request = new SemanticModelQueryRequest("What tables store customer data?");
var result = await queryService.QueryAsync(request);

Console.WriteLine(result.Answer);
Console.WriteLine($"Found {result.ReferencedEntities.Count} entities");
Console.WriteLine($"Rounds: {result.ResponseRounds}, Tokens: {result.TotalTokens}");

// Or use streaming
var streamingResult = await queryService.QueryStreamingAsync(request);
await using (streamingResult)
{
    await foreach (var token in streamingResult.Tokens)
    {
        Console.Write(token);
    }

    var metadata = await streamingResult.GetMetadataAsync();
    Console.WriteLine($"\nRounds: {metadata.ResponseRounds}, Tokens: {metadata.TotalTokens}");
}
```

## Error Scenarios

| Scenario | Behavior |
|----------|----------|
| No vector embeddings generated | Error: "Vector embeddings have not been generated. Run `generate-vectors` first." |
| Foundry endpoint unreachable | Error: "AI service unavailable. Check FoundryModels configuration." |
| Empty or whitespace question | Error: "Question must not be empty." |
| Token budget exceeded mid-query | Partial answer returned with `TerminationReason.TokenBudgetExceeded` |
| Timeout exceeded mid-query | Partial answer returned with `TerminationReason.TimeLimitExceeded` |

## Architecture

```
CLI (QueryModelCommandHandler)
  │
  ├── Parses --project, --question
  ├── Delegates to ISemanticModelQueryService
  └── Streams tokens to console
        │
        ▼
Core (SemanticModelQueryService : IAsyncDisposable)
  │
  ├── Creates AIAgent via IChatClientFactory → OpenAI ChatClient (once, on init)
  │     ├── searchTables
  │     ├── searchViews
  │     └── searchStoredProcedures
  │
  ├── Per query:
  │     ├── Creates AgentSession
  │     ├── Calls agent.RunStreamingAsync(question, session)
  │     ├── Monitors guardrails (time, tokens, rounds)
  │     └── Collects answer + referenced entities (deduplicated)
  │
  └── On dispose: releases agent reference
        │
        ▼
Core (SemanticModelSearchService)
  │
  ├── IEmbeddingGenerator → embedding vector
  ├── IVectorSearchService → ranked results (3x over-fetch)
  └── Entity type filtering → typed results
```
