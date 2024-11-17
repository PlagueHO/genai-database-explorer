using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// Gets or sets the chat completion settings.
    /// </summary>
    public required ChatCompletionSettings ChatCompletion { get; set; }

    /// <summary>
    /// Gets or sets the embedding settings.
    /// </summary>
    public required EmbeddingSettings Embedding { get; set; }
}