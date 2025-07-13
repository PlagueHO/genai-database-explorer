using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Configuration settings for the semantic model repository.
/// Defines persistence strategy and related configuration options.
/// </summary>
public class SemanticModelRepositorySettings
{
    /// <summary>
    /// The settings key that contains the SemanticModelRepository settings.
    /// </summary>
    public const string PropertyName = "SemanticModelRepository";

    /// <summary>
    /// Gets or sets the configuration for local disk persistence strategy.
    /// </summary>
    public LocalDiskConfiguration? LocalDisk { get; set; }

    /// <summary>
    /// Gets or sets the configuration for Azure Blob Storage persistence strategy.
    /// </summary>
    public AzureBlobStorageConfiguration? AzureBlobStorage { get; set; }

    /// <summary>
    /// Gets or sets the configuration for Azure Cosmos DB persistence strategy.
    /// </summary>
    public CosmosDbConfiguration? CosmosDb { get; set; }
}
