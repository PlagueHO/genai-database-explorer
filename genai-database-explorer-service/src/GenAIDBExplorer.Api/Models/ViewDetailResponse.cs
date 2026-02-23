namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Full detail response for a view including columns and SQL definition.
/// </summary>
public record ViewDetailResponse(
    string Schema,
    string Name,
    string? Description,
    string? SemanticDescription,
    DateTime? SemanticDescriptionLastUpdate,
    string? AdditionalInformation,
    string Definition,
    bool NotUsed,
    string? NotUsedReason,
    IReadOnlyList<ColumnResponse> Columns
);
