namespace GenAIDBExplorer.Core.PromptTemplates;

/// <summary>
/// Model configuration extracted from YAML frontmatter of a prompt template file.
/// </summary>
public sealed record PromptTemplateModelParameters(
    double? Temperature = null,
    double? TopP = null,
    int? MaxTokens = null
);
