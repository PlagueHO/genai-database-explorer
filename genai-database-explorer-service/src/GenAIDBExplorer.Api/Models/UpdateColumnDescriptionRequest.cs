namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Request body for updating column descriptions via PATCH.
/// </summary>
public record UpdateColumnDescriptionRequest(
    string? Description,
    string? SemanticDescription
);
