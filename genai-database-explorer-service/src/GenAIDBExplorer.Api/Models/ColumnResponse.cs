namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Response DTO for a column within a table or view.
/// </summary>
public record ColumnResponse(
    string Name,
    string? Type,
    string? Description,
    bool IsPrimaryKey,
    bool IsNullable,
    bool IsIdentity,
    bool IsComputed,
    bool IsXmlDocument,
    int? MaxLength,
    int? Precision,
    int? Scale,
    string? ReferencedTable,
    string? ReferencedColumn
);
