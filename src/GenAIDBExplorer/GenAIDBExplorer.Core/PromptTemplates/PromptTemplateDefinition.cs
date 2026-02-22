namespace GenAIDBExplorer.Core.PromptTemplates;

/// <summary>
/// Represents a fully parsed prompt template file (.prompt), containing
/// extracted YAML metadata and an ordered list of role-delimited messages.
/// </summary>
public sealed record PromptTemplateDefinition(
    string Name,
    string? Description,
    PromptTemplateModelParameters ModelParameters,
    IReadOnlyList<PromptTemplateMessage> Messages
);
