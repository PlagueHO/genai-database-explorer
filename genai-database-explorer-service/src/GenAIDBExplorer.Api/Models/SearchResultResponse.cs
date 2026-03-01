namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// A single matched semantic model entity returned in a search response.
/// </summary>
/// <param name="EntityType">The entity type: <c>"Table"</c>, <c>"View"</c>, or <c>"StoredProcedure"</c>.</param>
/// <param name="Schema">The database schema name (e.g., <c>"SalesLT"</c>).</param>
/// <param name="Name">The entity name (e.g., <c>"Customer"</c>).</param>
/// <param name="Description">The content description indexed for vector search.</param>
/// <param name="Score">The cosine similarity relevance score (0.0–1.0, higher is more relevant).</param>
public record SearchResultResponse(
    string EntityType,
    string Schema,
    string Name,
    string Description,
    double Score
);
