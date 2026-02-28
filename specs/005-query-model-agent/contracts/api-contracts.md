# API Contracts: Query Model Services

**Date**: 2026-02-28
**Spec**: [../spec.md](../spec.md)

## ISemanticModelQueryService

Primary service interface for agent-powered semantic model queries. Located in `GenAIDBExplorer.Core`.

```csharp
namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// Orchestrates agent-powered natural language queries against a semantic model's vector index.
/// Creates and manages a Foundry Agent Service agent with function tools for semantic model search.
/// Implements IAsyncDisposable to clean up the agent version on shutdown.
/// </summary>
public interface ISemanticModelQueryService : IAsyncDisposable
{
    /// <summary>
    /// Execute a natural language query and return the complete result.
    /// The agent uses function tools to search the semantic model vector index,
    /// iteratively reasoning over results until it can answer or a guardrail fires.
    /// </summary>
    Task<SemanticModelQueryResult> QueryAsync(
        SemanticModelQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a natural language query and stream answer tokens as they arrive.
    /// Returns a wrapper providing both the token stream and post-stream metadata.
    /// </summary>
    Task<SemanticModelStreamingQueryResult> QueryStreamingAsync(
        SemanticModelQueryRequest request,
        CancellationToken cancellationToken = default);
}
```

## ISemanticModelSearchService

Internal search service used by the agent's function tools. Located in `GenAIDBExplorer.Core`.

```csharp
namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// Provides vector-based search across semantic model entities with entity type filtering.
/// Used as the backing implementation for the agent's function tools.
/// </summary>
public interface ISemanticModelSearchService
{
    /// <summary>
    /// Search for tables matching the query using vector similarity.
    /// </summary>
    Task<IReadOnlyList<SemanticModelSearchResult>> SearchTablesAsync(
        string query, int topK, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for views matching the query using vector similarity.
    /// </summary>
    Task<IReadOnlyList<SemanticModelSearchResult>> SearchViewsAsync(
        string query, int topK, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for stored procedures matching the query using vector similarity.
    /// </summary>
    Task<IReadOnlyList<SemanticModelSearchResult>> SearchStoredProceduresAsync(
        string query, int topK, CancellationToken cancellationToken = default);
}
```

## Streaming Result Wrapper

```csharp
namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// Wrapper returned by QueryStreamingAsync. Provides access to the token stream
/// and, once the stream completes, the full query metadata.
/// </summary>
public sealed class SemanticModelStreamingQueryResult : IAsyncDisposable
{
    /// <summary>
    /// Stream of answer text tokens for real-time display.
    /// Must be fully enumerated before calling GetMetadataAsync.
    /// </summary>
    public IAsyncEnumerable<string> Tokens { get; }

    /// <summary>
    /// Returns the full query result (entities, rounds, token usage, termination reason)
    /// after the Tokens stream has been fully consumed.
    /// </summary>
    public Task<SemanticModelQueryResult> GetMetadataAsync();

    public ValueTask DisposeAsync();
}
```

## Data Transfer Objects

```csharp
namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// A natural language query request.
/// </summary>
public sealed record SemanticModelQueryRequest(
    string Question,
    int? TopK = null);

/// <summary>
/// The structured result of an agent-powered query.
/// </summary>
public sealed record SemanticModelQueryResult(
    string Answer,
    IReadOnlyList<SemanticModelSearchResult> ReferencedEntities,
    int ResponseRounds,
    long InputTokens,
    long OutputTokens,
    long TotalTokens,
    TimeSpan Duration,
    QueryTerminationReason TerminationReason);

/// <summary>
/// A single entity match from vector similarity search.
/// </summary>
public sealed record SemanticModelSearchResult(
    string EntityType,
    string SchemaName,
    string EntityName,
    string Content,
    double Score);

/// <summary>
/// Describes why the agent query loop terminated.
/// </summary>
public enum QueryTerminationReason
{
    Completed,
    MaxRoundsReached,
    TokenBudgetExceeded,
    TimeLimitExceeded,
    Error
}
```

## Settings Contract

```csharp
namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Configuration for the query-model agent and guardrails.
/// </summary>
public class QueryModelSettings
{
    public const string PropertyName = "QueryModel";

    public string AgentName { get; set; } = "genaidb-query-agent";
    public string? AgentInstructions { get; set; }
    public int MaxResponseRounds { get; set; } = 10;
    public long MaxTokenBudget { get; set; } = 100_000;
    public int TimeoutSeconds { get; set; } = 60;
    public int DefaultTopK { get; set; } = 5;
}
```

## DI Registration Contract

New registrations added to `HostBuilderExtensions.ConfigureServices()`:

```csharp
// Query model services
services.AddSingleton<ISemanticModelSearchService, SemanticModelSearchService>();
services.AddSingleton<ISemanticModelQueryService, SemanticModelQueryService>();
```

The `SemanticModelQueryService` constructor requires:
- `IProject` — for settings access
- `ISemanticModelSearchService` — for function tool implementations
- `IVectorInfrastructureFactory` — for vector infrastructure
- `ILogger<SemanticModelQueryService>` — for structured logging
