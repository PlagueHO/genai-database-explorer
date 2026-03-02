namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Response wrapper returned by the POST /api/search endpoint.
/// </summary>
/// <param name="Results">Ranked list of matching semantic model entities (highest score first). May be empty.</param>
/// <param name="TotalResults">Number of results returned (0–10).</param>
public record SearchResponse(
    IReadOnlyList<SearchResultResponse> Results,
    int TotalResults
);
