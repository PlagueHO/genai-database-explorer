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
    /// Gets or sets the configuration for Azure Blob persistence strategy.
    /// </summary>
    public AzureBlobConfiguration? AzureBlob { get; set; }

    /// <summary>
    /// Gets or sets the configuration for Azure Cosmos DB persistence strategy.
    /// </summary>
    public CosmosDbConfiguration? CosmosDb { get; set; }

    /// <summary>
    /// Gets or sets the lazy loading configuration for semantic model entities.
    /// </summary>
    public LazyLoadingConfiguration LazyLoading { get; set; } = new();

    /// <summary>
    /// Gets or sets the caching configuration for semantic models.
    /// </summary>
    public CachingConfiguration Caching { get; set; } = new();

    /// <summary>
    /// Gets or sets the change tracking configuration for selective persistence.
    /// </summary>
    public ChangeTrackingConfiguration ChangeTracking { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance monitoring configuration.
    /// </summary>
    public PerformanceMonitoringConfiguration PerformanceMonitoring { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum number of concurrent operations allowed.
    /// </summary>
    [Range(1, 50)]
    public int MaxConcurrentOperations { get; set; } = 10;
}

/// <summary>
/// Configuration for lazy loading behavior of semantic model entities.
/// </summary>
public class LazyLoadingConfiguration
{
    /// <summary>
    /// Gets or sets whether lazy loading is enabled for entity collections.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Configuration for caching behavior of semantic models.
/// </summary>
public class CachingConfiguration
{
    /// <summary>
    /// Gets or sets whether caching is enabled for loaded models.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache expiration time in minutes.
    /// </summary>
    [Range(1, 1440)] // 1 minute to 24 hours
    public int ExpirationMinutes { get; set; } = 30;
}

/// <summary>
/// Configuration for change tracking behavior for selective persistence.
/// </summary>
public class ChangeTrackingConfiguration
{
    /// <summary>
    /// Gets or sets whether change tracking is enabled for selective persistence.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Configuration for performance monitoring.
/// </summary>
public class PerformanceMonitoringConfiguration
{
    /// <summary>
    /// Gets or sets whether performance monitoring is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether detailed timing information should be collected.
    /// </summary>
    public bool DetailedTiming { get; set; } = false;

    /// <summary>
    /// Gets or sets whether metrics collection is enabled.
    /// </summary>
    public bool MetricsEnabled { get; set; } = true;
}
