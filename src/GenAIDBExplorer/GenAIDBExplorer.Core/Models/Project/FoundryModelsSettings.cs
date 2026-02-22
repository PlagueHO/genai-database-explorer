using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class FoundryModelsSettings()
{
    // The settings key that contains the FoundryModels settings
    public const string PropertyName = "FoundryModels";

    [Required]
    public FoundryModelsDefaultSettings Default { get; set; } = new FoundryModelsDefaultSettings();

    /// <summary>
    /// Gets or sets the chat completion settings.
    /// </summary>
    [Required]
    public ChatCompletionDeploymentSettings ChatCompletion { get; set; } = new ChatCompletionDeploymentSettings();

    /// <summary>
    /// Gets or sets the structured chat completion settings.
    /// </summary>
    [Required]
    public ChatCompletionStructuredDeploymentSettings ChatCompletionStructured { get; set; } = new ChatCompletionStructuredDeploymentSettings();

    /// <summary>
    /// Gets or sets the embedding settings.
    /// </summary>
    [Required]
    public EmbeddingDeploymentSettings Embedding { get; set; } = new EmbeddingDeploymentSettings();
}
