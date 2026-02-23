namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Full detail response for a table including columns and indexes.
/// </summary>
public record TableDetailResponse(
    string Schema,
    string Name,
    string? Description,
    string? SemanticDescription,
    DateTime? SemanticDescriptionLastUpdate,
    string? Details,
    string? AdditionalInformation,
    bool NotUsed,
    string? NotUsedReason,
    IReadOnlyList<ColumnResponse> Columns,
    IReadOnlyList<IndexResponse> Indexes
);
