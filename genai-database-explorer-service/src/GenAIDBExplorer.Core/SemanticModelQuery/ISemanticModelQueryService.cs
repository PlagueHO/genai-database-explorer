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
