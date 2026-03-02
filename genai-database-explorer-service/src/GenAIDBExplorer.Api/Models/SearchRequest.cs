namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Request body for the POST /api/search endpoint.
/// </summary>
/// <param name="Query">The natural language search query. Required, non-empty, max 2000 characters.</param>
/// <param name="Limit">Maximum number of results to return (1–10, default 10). Values above 10 are clamped to 10.</param>
/// <param name="EntityTypes">
/// Optional array of entity types to filter results.
/// Valid values are <c>"table"</c>, <c>"view"</c>, and <c>"storedProcedure"</c>.
/// When omitted or <c>null</c>, all entity types are searched.
/// </param>
public record SearchRequest(
    string Query,
    int? Limit,
    IReadOnlyList<string>? EntityTypes
);
