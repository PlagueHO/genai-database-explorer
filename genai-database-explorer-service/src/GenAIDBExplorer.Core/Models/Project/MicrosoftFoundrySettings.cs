using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class MicrosoftFoundrySettings()
{
    // The settings key that contains the MicrosoftFoundry settings
    public const string PropertyName = "MicrosoftFoundry";

    [Required]
    public MicrosoftFoundryDefaultSettings Default { get; set; } = new MicrosoftFoundryDefaultSettings();

    /// <summary>
    /// Gets or sets the chat completion settings.
    /// </summary>
    [Required]
    public ChatCompletionDeploymentSettings ChatCompletion { get; set; } = new ChatCompletionDeploymentSettings();

    /// <summary>
    /// Gets or sets the embedding settings.
    /// </summary>
    [Required]
    public EmbeddingDeploymentSettings Embedding { get; set; } = new EmbeddingDeploymentSettings();
}
