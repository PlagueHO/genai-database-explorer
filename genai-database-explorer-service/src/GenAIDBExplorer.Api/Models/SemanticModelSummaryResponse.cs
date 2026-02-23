namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Summary response for the entire semantic model.
/// </summary>
public record SemanticModelSummaryResponse(
    string Name,
    string Source,
    string? Description,
    int TableCount,
    int ViewCount,
    int StoredProcedureCount
);
