namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// A single entity match from vector similarity search.
/// </summary>
/// <param name="EntityType">The entity type: "Table", "View", or "StoredProcedure".</param>
/// <param name="SchemaName">The database schema name (e.g., "SalesLT").</param>
/// <param name="EntityName">The entity name (e.g., "Customer").</param>
/// <param name="Content">The content description that was indexed.</param>
/// <param name="Score">The cosine similarity score (0.0–1.0).</param>
public sealed record SemanticModelSearchResult(
    string EntityType,
    string SchemaName,
    string EntityName,
    string Content,
    double Score);
