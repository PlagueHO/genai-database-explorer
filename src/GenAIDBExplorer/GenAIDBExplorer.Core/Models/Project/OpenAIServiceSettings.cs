using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class OpenAIServiceSettings()
{
    // The settings key that contains the OpenAIService settings
    public const string PropertyName = "OpenAIService";

    [Required]
    public OpenAIServiceDefaultSettings Default { get; set; } = new OpenAIServiceDefaultSettings();
    /// <summary>
    /// Gets or sets the chat completion settings.
    /// </summary>
    [Required]
    public OpenAIServiceChatCompletionSettings ChatCompletion { get; set; } = new OpenAIServiceChatCompletionSettings();

    /// <summary>
    /// Gets or sets the chat completion settings.
    /// </summary>
    [Required]
    public OpenAIServiceChatCompletionStructuredSettings ChatCompletionStructured { get; set; } = new OpenAIServiceChatCompletionStructuredSettings();

    /// <summary>
    /// Gets or sets the embedding settings.
    /// </summary>
    [Required]
    public OpenAIServiceEmbeddingSettings Embedding { get; set; } = new OpenAIServiceEmbeddingSettings();
}
