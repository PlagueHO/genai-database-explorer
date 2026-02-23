namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Full detail response for a stored procedure including parameters and SQL definition.
/// </summary>
public record StoredProcedureDetailResponse(
    string Schema,
    string Name,
    string? Description,
    string? SemanticDescription,
    DateTime? SemanticDescriptionLastUpdate,
    string? AdditionalInformation,
    string? Parameters,
    string Definition,
    bool NotUsed,
    string? NotUsedReason
);
