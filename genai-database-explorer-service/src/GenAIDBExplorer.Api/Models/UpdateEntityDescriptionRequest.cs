namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Request body for updating entity descriptions via PATCH.
/// </summary>
public record UpdateEntityDescriptionRequest(
    string? Description,
    string? SemanticDescription,
    bool? NotUsed,
    string? NotUsedReason
);
