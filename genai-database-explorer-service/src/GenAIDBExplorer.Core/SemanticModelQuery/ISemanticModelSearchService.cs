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

    /// <summary>
    /// Search across all semantic model entity types using a single vector similarity operation.
    /// Generates one embedding for the query, performs one vector search, then filters and ranks results.
    /// </summary>
    /// <param name="query">The natural language search query.</param>
    /// <param name="topK">Maximum number of results to return (1–10; values above 10 are capped at 10).</param>
    /// <param name="entityTypes">
    /// Optional list of entity types to filter by (e.g., <c>"Table"</c>, <c>"View"</c>, <c>"StoredProcedure"</c>).
    /// Pass <c>null</c> or an empty list to search all entity types.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ranked list of matching entities ordered by relevance score descending.</returns>
    Task<IReadOnlyList<SemanticModelSearchResult>> SearchAsync(
        string query,
        int topK,
        IReadOnlyList<string>? entityTypes,
        CancellationToken cancellationToken = default);
}
