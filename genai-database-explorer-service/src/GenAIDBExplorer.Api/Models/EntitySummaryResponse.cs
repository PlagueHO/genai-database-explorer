namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Summary representation of a database entity (table, view, stored procedure) for list endpoints.
/// </summary>
public record EntitySummaryResponse(
    string Schema,
    string Name,
    string? Description,
    string? SemanticDescription,
    bool NotUsed
);
