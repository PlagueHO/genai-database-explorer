using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Represents the settings for a project.
/// </summary>
public class ProjectSettings
{
    /// <summary>
    /// Gets or sets the version of the settings.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public Version? SettingsVersion { get; set; }

    /// <summary>
    /// Gets or sets the database settings.
    /// </summary>
    public required DatabaseSettings Database { get; set; }


    /// <summary>
    /// Gets or sets the data dictionary settings.
    /// </summary>
    public required DataDictionarySettings DataDictionary { get; set; }

    /// <summary>
    /// Gets or sets the semantic model settings.
    /// </summary>
    public required SemanticModelSettings SemanticModel { get; set; }

    /// <summary>
    /// Gets or sets the chat completion settings.
    /// </summary>
    public required ChatCompletionSettings ChatCompletion { get; set; }

    /// <summary>
    /// Gets or sets the chat completion settings.
    /// </summary>
    public required ChatCompletionStructuredSettings ChatCompletionStructured { get; set; }

    /// <summary>
    /// Gets or sets the embedding settings.
    /// </summary>
    public required EmbeddingSettings Embedding { get; set; }
}