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
