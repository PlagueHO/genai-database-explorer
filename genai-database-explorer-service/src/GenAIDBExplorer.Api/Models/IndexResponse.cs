namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Response DTO for an index within a table.
/// </summary>
public record IndexResponse(
    string Name,
    string? Type,
    string? ColumnName,
    bool IsUnique,
    bool IsPrimaryKey,
    bool IsUniqueConstraint
);
