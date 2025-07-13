using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Configuration options for local disk persistence strategy.
/// </summary>
public sealed class LocalDiskConfiguration
{
    /// <summary>
    /// Configuration section name for local disk settings.
    /// </summary>
    public const string SectionName = "SemanticModelRepository:LocalDisk";

    /// <summary>
    /// Gets or sets the directory path for storing semantic model files.
    /// Default is "SemanticModel".
    /// </summary>
    [Required]
    [NotEmptyOrWhitespace]
    public required string Directory { get; set; } = "SemanticModel";
}
