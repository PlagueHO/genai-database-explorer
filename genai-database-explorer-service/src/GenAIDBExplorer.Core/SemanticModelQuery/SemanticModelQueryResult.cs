namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// The structured result of an agent-powered query.
/// </summary>
/// <param name="Answer">The agent's synthesized natural language answer.</param>
/// <param name="ReferencedEntities">Entities discovered during search (may be empty).</param>
/// <param name="ResponseRounds">Number of response rounds executed.</param>
/// <param name="InputTokens">Total input tokens consumed.</param>
/// <param name="OutputTokens">Total output tokens consumed.</param>
/// <param name="TotalTokens">Total tokens consumed.</param>
/// <param name="Duration">Wall-clock duration of the query.</param>
/// <param name="TerminationReason">Why the query loop ended.</param>
public sealed record SemanticModelQueryResult(
    string Answer,
    IReadOnlyList<SemanticModelSearchResult> ReferencedEntities,
    int ResponseRounds,
    long InputTokens,
    long OutputTokens,
    long TotalTokens,
    TimeSpan Duration,
    QueryTerminationReason TerminationReason);
